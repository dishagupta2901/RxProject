import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { OrderForm } from './OrderForm';

describe('OrderForm', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows a field-level validation message instead of submitting when a numeric field is not a number', async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    const onSubmitted = vi.fn();
    const user = userEvent.setup();
    render(<OrderForm token="a-token" onSubmitted={onSubmitted} />);

    const sphereInput = screen.getByLabelText('Sphere');
    await user.clear(sphereInput);
    await user.type(sphereInput, 'not-a-number');
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    expect(await screen.findByText('Sphere must be a number.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    expect(onSubmitted).not.toHaveBeenCalled();
  });

  it('submits and reports the backend result on success', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ orderId: 'order-1', price: 110, status: 'Submitted' }), {
          status: 202,
          headers: { 'content-type': 'application/json' },
        }),
      ),
    );
    const onSubmitted = vi.fn();
    const user = userEvent.setup();
    render(<OrderForm token="a-token" onSubmitted={onSubmitted} />);

    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await vi.waitFor(() =>
      expect(onSubmitted).toHaveBeenCalledWith({ orderId: 'order-1', price: 110, status: 'Submitted' }),
    );
  });

  it('surfaces the backend error message when submission is rejected', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ error: 'Frame id is required.' }), {
          status: 400,
          headers: { 'content-type': 'application/json' },
        }),
      ),
    );
    const user = userEvent.setup();
    render(<OrderForm token="a-token" onSubmitted={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: /submit order/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Frame id is required.');
  });
});
