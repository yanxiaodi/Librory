import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { LandingPage } from './LandingPage'

describe('LandingPage', () => {
  it('shows the public brand and sign in entry point', () => {
    render(
      <MemoryRouter>
        <LandingPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /librory/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /sign in to librory/i })).toBeVisible()
    expect(screen.getByText(/scan before you buy/i)).toBeVisible()
  })
})
