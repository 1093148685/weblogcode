const CHAT_MODE_LABELS = {
  auto: '智能模式',
  normal: '普通聊天',
  rag: '知识库问答',
  web: '联网搜索'
}

export const buildSidebarSections = () => ({
  primary: ['new-chat', 'search', 'history'],
  secondary: ['settings']
})

const parseSessionDate = (raw, now) => {
  if (!raw) return null

  const date = raw instanceof Date ? raw : new Date(raw)
  if (!Number.isNaN(date.getTime())) return date

  if (typeof raw !== 'string') return null

  const displayDateMatch = raw.trim().match(/^(\d{1,2})月(\d{1,2})日\s+(\d{1,2}):(\d{2})$/)
  if (!displayDateMatch) return null

  const [, month, day, hour, minute] = displayDateMatch
  const displayDate = new Date(
    now.getFullYear(),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute)
  )

  return Number.isNaN(displayDate.getTime()) ? null : displayDate
}

const getSessionDate = (session, now) => {
  const raw = session?.updatedAt ?? session?.createdAt ?? session?.time ?? session?.timestamp
  const parsedDate = parseSessionDate(raw, now)
  if (parsedDate) return parsedDate

  const idTimestamp = typeof session?.id === 'string' ? session.id.match(/session_(\d+)/)?.[1] : null
  if (!idTimestamp) return null

  const idDate = new Date(Number(idTimestamp))
  return Number.isNaN(idDate.getTime()) ? null : idDate
}

const startOfLocalDay = (date) => new Date(date.getFullYear(), date.getMonth(), date.getDate())

const getDayBucket = (date, now) => {
  const diffDays = Math.floor((startOfLocalDay(now) - startOfLocalDay(date)) / 86400000)

  if (diffDays === 0) return '今天'
  if (diffDays === 1) return '昨天'
  return '更早'
}

export const groupSessionsForSidebar = (sessions, now = new Date()) => {
  const grouped = new Map([
    ['今天', []],
    ['昨天', []],
    ['更早', []]
  ])

  const orderedSessions = [...(sessions || [])]
    .map((session, index) => ({ session, index, date: getSessionDate(session, now) }))
    .filter(item => item.date)
    .sort((a, b) => b.date - a.date || a.index - b.index)

  for (const { session, date } of orderedSessions) {
    grouped.get(getDayBucket(date, now)).push(session)
  }

  return Array.from(grouped.entries())
    .filter(([, items]) => items.length > 0)
    .map(([label, items]) => ({ label, items }))
}

export const buildTopBarState = ({ modelName, chatMode, selectedKbName, webEnabled } = {}) => {
  const primary = []
  const secondary = []

  return { primary, secondary }
}

export const buildComposerToolState = ({ chatMode, selectedKbName, webEnabled } = {}) => {
  const webActive = typeof webEnabled === 'boolean' ? webEnabled : chatMode === 'web'

  return [
    {
      key: 'mode',
      label: '模式',
      value: CHAT_MODE_LABELS[chatMode] || CHAT_MODE_LABELS.normal
    },
    {
      key: 'kb',
      label: '知识库',
      active: Boolean(selectedKbName),
      value: selectedKbName || ''
    },
    {
      key: 'web',
      label: '联网',
      active: webActive
    }
  ]
}
