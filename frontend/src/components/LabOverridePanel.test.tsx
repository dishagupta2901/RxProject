import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LabOverridePanel } from './LabOverridePanel';

describe('LabOverridePanel', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('honestly reports that the backend has no lab-override route yet, instead of faking success', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 404 })));
    const user = userEvent.setup();
    render(<LabOverridePanel token="a-token" orderId="order-1" />);

    await user.type(screen.getByLabelText('Reason'), 'grinding tolerance exceeded');
    await user.click(screen.getByRole('button', { name: /request lab override/i }));

    expect(await screen.findByText(/does not implement a lab-override endpoint yet/i)).toBeInTheDocument();
  });

  it('warns when the supplied token has no lab-override role', () => {
    render(<LabOverridePanel token="" orderId="order-1" />);

    expect(screen.getByText(/does not carry the/i)).toBeInTheDocument();
  });
});
