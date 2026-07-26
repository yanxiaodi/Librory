import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import LoginPage from './LoginPage'

describe('LoginPage', () => {
  it('shows the three sign in choices', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeVisible()
    expect(screen.getByRole('button', { name: /continue with google/i })).toBeVisible()
    expect(screen.getByRole('button', { name: /continue with microsoft/i })).toBeVisible()
    expect(screen.getByRole('button', { name: /continue with email/i })).toBeVisible()
  })
})
