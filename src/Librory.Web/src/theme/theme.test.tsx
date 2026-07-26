import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { ThemeRoot } from './ThemeRoot'

describe('ThemeRoot', () => {
  it('loads Botanical Press when nothing is saved and writes it to localStorage', () => {
    render(
      <MemoryRouter>
        <ThemeRoot>
          <div>test</div>
        </ThemeRoot>
      </MemoryRouter>,
    )

    expect(localStorage.getItem('librory.theme')).toBe('botanical-press')
  })
})
