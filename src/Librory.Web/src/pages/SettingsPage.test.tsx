import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import SettingsPage from './SettingsPage'
import { ThemeRoot } from '@/theme/ThemeRoot'

describe('SettingsPage', () => {
  it('changes theme selection from the style dropdown', async () => {
    const user = userEvent.setup()

    render(
      <MemoryRouter>
        <ThemeRoot>
          <SettingsPage />
        </ThemeRoot>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: /botanical press/i }))
    await user.click(screen.getByRole('option', { name: /cozy archive/i }))

    expect(screen.getByRole('button', { name: /cozy archive/i })).toBeVisible()
  })
})
