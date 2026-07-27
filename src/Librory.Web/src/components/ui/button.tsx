import * as React from 'react'
import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const buttonVariants = cva(
  [
    'inline-flex items-center justify-center gap-2.5 whitespace-nowrap',
    'rounded-[var(--radius-md)]',
    'font-[family-name:var(--font-display)] font-normal italic',
    'transition-all duration-[var(--duration-normal)] ease-out',
    'active:scale-[0.985]',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--accent-subtle)]',
    'disabled:pointer-events-none disabled:opacity-50',
  ].join(' '),
  {
    variants: {
      variant: {
        default: 'bg-[var(--accent)] text-[var(--accent-on-accent)] hover:bg-[color-mix(in_srgb,var(--accent)_90%,black)]',
        outline: 'border border-[var(--border-subtle)] bg-[var(--surface-elevated)] text-[var(--text-primary)] hover:bg-[var(--surface-sunken)] active:bg-[var(--surface-sunken)]',
      },
      size: {
        default: 'h-12 px-4 text-[15px]',
        lg: 'h-[52px] w-full justify-center px-5 text-[17px]',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button'
    return <Comp ref={ref} className={cn(buttonVariants({ variant, size, className }))} {...props} />
  }
)

Button.displayName = 'Button'

export { Button }
