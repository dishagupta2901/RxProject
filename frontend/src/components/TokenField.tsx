interface TokenFieldProps {
  token: string;
  onChange: (token: string) => void;
}

/**
 * Local-fixture bearer token entry. There is no dev token issuer wired up yet (open item —
 * DECISIONS.md D-007 is still "proposed"), so this is a plain input: the optician pastes whatever
 * token their local auth fixture issues.
 */
export function TokenField({ token, onChange }: TokenFieldProps) {
  return (
    <label className="token-field">
      Bearer token
      <input
        type="password"
        autoComplete="off"
        value={token}
        onChange={(event) => onChange(event.target.value)}
        placeholder="paste a local auth fixture token"
      />
    </label>
  );
}
