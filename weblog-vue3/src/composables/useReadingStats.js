/**
 * 根据 HTML 内容计算总字数和预估阅读时长
 */
export function useReadingStats(content) {
    return {
        totalWords: 0,
        readTime: '--'
    }
}

export function computeReadingStats(htmlContent) {
    if (!htmlContent || typeof htmlContent !== 'string') {
        return { totalWords: 0, readTime: '< 1 分钟' }
    }

    // 去除 HTML 标签
    const text = htmlContent
        .replace(/<[^>]*>/g, '')
        .replace(/&nbsp;/g, ' ')
        .replace(/&lt;/g, '<')
        .replace(/&gt;/g, '>')
        .replace(/&amp;/g, '&')
        .replace(/&#?\w+;/g, '')
        .trim()

    if (!text) {
        return { totalWords: 0, readTime: '< 1 分钟' }
    }

    // 统计中文字符（包括中文标点不计入）
    const chineseChars = (text.match(/[一-鿿㐀-䶿]/g) || []).length

    // 统计英文单词（按空白分隔的词组）
    const englishOnly = text.replace(/[一-鿿㐀-䶿]/g, ' ').trim()
    const englishWords = englishOnly ? englishOnly.split(/\s+/).filter(w => /[a-zA-Z0-9]/.test(w)).length : 0

    // 统计数字序列
    const numbers = (text.match(/\d+/g) || []).length

    const totalWords = chineseChars + englishWords + numbers

    // 中文约 300 字/分钟，英文约 200 词/分钟
    // 混合内容按中间值 ~260 字/分钟估算
    const minutes = Math.ceil(totalWords / 260)

    return {
        totalWords,
        readTime: minutes < 1 ? '< 1 分钟' : `约 ${minutes} 分钟`
    }
}
