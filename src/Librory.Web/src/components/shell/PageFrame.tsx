import * as React from 'react'
import { cn } from '@/lib/utils'

type PageFrameProps = {
  eyebrow: string
  title: string
  description: string
  children?: React.ReactNode
}

export function PageFrame({ eyebrow, title, description, children }: PageFrameProps) {
  return (
    <section
      className={cn(
        'rounded-[24px] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-5 py-5 shadow-none',
        'sm:px-6 sm:py-6',
      )}
    >
      <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">{eyebrow}</p>
      <h2 className="mt-2 font-[family-name:var(--font-display)] text-[1.7rem] font-normal italic tracking-[-0.01em] text-[var(--text-primary)]">
        {title}
      </h2>
      <p className="mt-2 max-w-[34rem] font-[family-name:var(--font-body)] text-sm leading-6 text-[var(--text-secondary)]">
        {description}
      </p>
      {children ? <div className="mt-5">{children}</div> : null}
    </section>
  )
}
