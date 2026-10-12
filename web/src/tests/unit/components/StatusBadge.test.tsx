import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatusBadge } from '../../../components/StatusBadge';

describe('StatusBadge', () => {
  it('defaults recovered status to warning tone', () => {
    render(<StatusBadge status="Recovered" />);

    expect(screen.getByLabelText('Status Recovered')).toHaveClass('status-warning');
  });
});