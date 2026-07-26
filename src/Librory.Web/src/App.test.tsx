import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from './App'
import { ThemeRoot } from '@/theme/ThemeRoot'

describe('App shell', () => {
  it('shows the settings page and bottom navigation on the settings route', () => {
    render(
      <MemoryRouter initialEntries={['/settings']}>
        <ThemeRoot>
          <App />
        </ThemeRoot>
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /settings/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /home/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /scans/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /library/i })).toBeVisible()
  })
})
