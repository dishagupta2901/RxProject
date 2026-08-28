import { describe, expect, it } from 'vitest';
import { decodeJwtRoles } from './jwt';

function fakeJwt(payload: Record<string, unknown>): string {
  const base64url = (value: string) => btoa(value).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  const header = base64url(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const body = base64url(JSON.stringify(payload));
  return `${header}.${body}.`;
}

describe('decodeJwtRoles', () => {
  it('reads a single string role claim', () => {
    expect(decodeJwtRoles(fakeJwt({ role: 'lab-override' }))).toEqual(['lab-override']);
  });

  it('reads an array of roles claim', () => {
    expect(decodeJwtRoles(fakeJwt({ roles: ['optician', 'lab-override'] }))).toEqual(['optician', 'lab-override']);
  });

  it('returns an empty array when there is no role claim', () => {
    expect(decodeJwtRoles(fakeJwt({ sub: 'user-1' }))).toEqual([]);
  });

  it('returns an empty array for a malformed token instead of throwing', () => {
    expect(decodeJwtRoles('not-a-jwt')).toEqual([]);
    expect(decodeJwtRoles('')).toEqual([]);
  });
});
