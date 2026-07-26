import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import './index.css'
import { ThemeRoot } from '@/theme/ThemeRoot'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <ThemeRoot>
        <App />
      </ThemeRoot>
    </BrowserRouter>
  </React.StrictMode>,
)
