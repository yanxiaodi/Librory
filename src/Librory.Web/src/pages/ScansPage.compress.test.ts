import { afterEach, describe, expect, it, vi } from 'vitest'
import { compressImageToJpeg } from './ScansPage'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('compressImageToJpeg', () => {
  it('returns small jpeg files unchanged', async () => {
    const file = new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' })

    await expect(compressImageToJpeg(file)).resolves.toBe(file)
  })

  it('converts non-jpeg files to jpeg before upload', async () => {
    const createdUrls: string[] = []
    const revokedUrls: string[] = []
    const originalCreateObjectURL = URL.createObjectURL
    const originalRevokeObjectURL = URL.revokeObjectURL

    vi.stubGlobal('Image', class {
      width = 2400
      height = 1800
      onload: (() => void) | null = null
      onerror: (() => void) | null = null
      set src(_value: string) {
        queueMicrotask(() => this.onload?.())
      }
    } as unknown as typeof Image)

    vi.stubGlobal('document', {
      createElement: () => ({
        width: 0,
        height: 0,
        getContext: () => ({
          drawImage: vi.fn(),
        }),
        toBlob: (callback: (blob: Blob | null) => void) => callback(new Blob(['compressed'], { type: 'image/jpeg' })),
      }),
    } as unknown as Document)

    vi.stubGlobal('URL', {
      createObjectURL: (value: Blob) => {
        const url = `blob:${value.type}:${createdUrls.length + 1}`
        createdUrls.push(url)
        return url
      },
      revokeObjectURL: (value: string) => {
        revokedUrls.push(value)
      },
    } as unknown as typeof URL)

    const file = new File([new Uint8Array(11 * 1024 * 1024)], 'shelf.png', { type: 'image/png' })
    const result = await compressImageToJpeg(file)

    expect(result).not.toBe(file)
    expect(result.type).toBe('image/jpeg')
    expect(result.name).toBe('shelf.jpg')
    expect(createdUrls).toHaveLength(1)
    expect(revokedUrls).toEqual(createdUrls)

    URL.createObjectURL = originalCreateObjectURL
    URL.revokeObjectURL = originalRevokeObjectURL
  })
})