export function safeUploadFileName(file) {
  const fallbackExt = file?.type?.split('/')?.[1] || 'png'
  const originalName = file?.name || `image.${fallbackExt}`
  const dotIndex = originalName.lastIndexOf('.')
  const rawExt = dotIndex >= 0 ? originalName.slice(dotIndex + 1) : fallbackExt
  const ext = (rawExt || fallbackExt).replace(/[^a-zA-Z0-9]/g, '').toLowerCase() || fallbackExt
  const stamp = Date.now().toString(36)
  const random = Math.random().toString(36).slice(2, 8)

  return `upload-${stamp}-${random}.${ext}`
}
