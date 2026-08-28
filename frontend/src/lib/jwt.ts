/**
 * Best-effort, unverified decode of a JWT payload. This exists only to drive UI hints (e.g.
 * whether to surface the lab-override panel) — it never validates a signature and must never be
 * treated as an authorization decision. The backend's `LabOverride` authorization policy
 * (Program.cs, `RequireRole("lab-override")`) is the actual source of truth.
 */
export function decodeJwtRoles(token: string): string[] {
  const payload = decodeJwtPayload(token);
  if (!payload) return [];
  const role = payload.role ?? payload.roles;
  if (Array.isArray(role)) return role.filter((value): value is string => typeof value === 'string');
  if (typeof role === 'string') return [role];
  return [];
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;
  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/').padEnd(parts[1].length + ((4 - (parts[1].length % 4)) % 4), '=');
    const json = atob(base64);
    const parsed: unknown = JSON.parse(json);
    return typeof parsed === 'object' && parsed !== null ? (parsed as Record<string, unknown>) : null;
  } catch {
    return null;
  }
}
