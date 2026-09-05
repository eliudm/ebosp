// Centralized API client (frontend/src/services/README.md): auth headers, error normalization,
// and a single silent-refresh-then-retry path for expired access tokens (dev guide §11.1 step 7:
// "API validates identity on every protected request").

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5231'

export interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.title ?? `Request failed with status ${status}`)
    this.status = status
    this.problem = problem
  }
}

/**
 * GlobalExceptionHandler (src/EBOSP.Api/Middleware/GlobalExceptionHandler.cs) puts every typed
 * exception's actual message into ProblemDetails.Title, never .Detail (which this backend never
 * populates at all) - use this everywhere instead of reading .detail directly, so that mistake
 * doesn't get re-copied into every new form.
 */
export function getErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError && err.problem?.title) {
    return err.problem.title
  }
  return fallback
}

interface RequestOptions {
  method?: string
  body?: unknown
  /** Set false for endpoints that must not send a bearer token or trigger a refresh (login, tenant creation, ...). */
  authenticated?: boolean
}

type AccessTokenProvider = () => string | null
type RefreshHandler = () => Promise<boolean>
type SessionExpiredHandler = () => void

let getAccessToken: AccessTokenProvider = () => null
let refreshAccessToken: RefreshHandler = () => Promise.resolve(false)
let onSessionExpired: SessionExpiredHandler = () => {}
let refreshInFlight: Promise<boolean> | null = null

export function configureApiClient(config: {
  getAccessToken: AccessTokenProvider
  refresh: RefreshHandler
  onSessionExpired: SessionExpiredHandler
}): void {
  getAccessToken = config.getAccessToken
  refreshAccessToken = config.refresh
  onSessionExpired = config.onSessionExpired
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { authenticated = true } = options

  const response = await doFetch(path, options)

  if (response.status === 401 && authenticated) {
    const refreshed = await refreshOnce()
    if (refreshed) {
      return handleResponse<T>(await doFetch(path, options))
    }
    onSessionExpired()
  }

  return handleResponse<T>(response)
}

function doFetch(path: string, options: RequestOptions): Promise<Response> {
  const { method = 'GET', body, authenticated = true } = options
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }

  if (authenticated) {
    const token = getAccessToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  return fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

function refreshOnce(): Promise<boolean> {
  refreshInFlight ??= refreshAccessToken().finally(() => {
    refreshInFlight = null
  })
  return refreshInFlight
}

function authHeaders(): Record<string, string> {
  const token = getAccessToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

/** For multipart/form-data uploads - apiRequest always JSON-encodes the body, which breaks the multipart boundary. */
export async function apiUpload<T>(path: string, formData: FormData): Promise<T> {
  const doUpload = () => fetch(`${API_BASE_URL}${path}`, { method: 'POST', headers: authHeaders(), body: formData })

  const response = await doUpload()
  if (response.status === 401) {
    const refreshed = await refreshOnce()
    if (refreshed) {
      return handleResponse<T>(await doUpload())
    }
    onSessionExpired()
  }
  return handleResponse<T>(response)
}

/** For raw byte downloads - apiRequest always JSON-parses the response body. */
export async function apiDownloadBlob(path: string): Promise<Blob> {
  const doDownload = () => fetch(`${API_BASE_URL}${path}`, { method: 'GET', headers: authHeaders() })

  const response = await doDownload()
  if (response.status === 401) {
    const refreshed = await refreshOnce()
    if (refreshed) {
      return handleBlobResponse(await doDownload())
    }
    onSessionExpired()
  }
  return handleBlobResponse(response)
}

async function handleBlobResponse(response: Response): Promise<Blob> {
  if (!response.ok) {
    throw new ApiError(response.status, await safeParseJson<ProblemDetails>(response))
  }
  return response.blob()
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, await safeParseJson<ProblemDetails>(response))
  }

  if (response.status === 204 || response.status === 202) {
    return undefined as T
  }

  return (await safeParseJson<T>(response)) as T
}

async function safeParseJson<T>(response: Response): Promise<T | null> {
  const text = await response.text()
  if (!text) {
    return null
  }
  return JSON.parse(text) as T
}
