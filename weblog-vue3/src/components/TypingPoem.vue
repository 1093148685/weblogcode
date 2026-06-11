<template>
    <div class="typing-poem-container">
        <p class="typing-text text-[var(--text-secondary)]">
            <span>{{ displayedText }}</span>
            <span class="cursor" :class="{ 'cursor-blink': !isTyping }">|</span>
        </p>
    </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'

const props = defineProps({
    lines: {
        type: Array,
        default: () => [
            '有人说，每一次相遇都是久别重逢。',
            '你翻山越岭，穿过人海，来到这里。',
            '不如留下只言片语，',
            '让风替你记住这一刻。'
        ]
    },
    typingSpeed: {
        type: Number,
        default: 80
    },
    lineDelay: {
        type: Number,
        default: 600
    },
    loopDelay: {
        type: Number,
        default: 5000
    }
})

const displayedText = ref('')
const isTyping = ref(true)

let timer = null
let currentLineIndex = 0
let currentCharIndex = 0
let fullText = ''

function buildFullText() {
    return props.lines.join('\n')
}

function typeNext() {
    fullText = buildFullText()

    if (currentCharIndex < fullText.length) {
        const char = fullText[currentCharIndex]
        displayedText.value += char
        currentCharIndex++

        // If we just typed a newline, pause a bit longer
        if (char === '\n') {
            timer = setTimeout(typeNext, props.lineDelay)
        } else {
            timer = setTimeout(typeNext, props.typingSpeed)
        }
    } else {
        // Finished typing
        isTyping.value = false
    }
}

function startTyping() {
    displayedText.value = ''
    currentCharIndex = 0
    isTyping.value = true
    typeNext()
}

onMounted(() => {
    startTyping()
})

onBeforeUnmount(() => {
    if (timer) clearTimeout(timer)
})
</script>

<style scoped>
.typing-poem-container {
    text-align: center;
    padding: 1.5rem 1rem;
}

.typing-text {
    font-size: 0.875rem;
    line-height: 2;
    letter-spacing: 0.5px;
    white-space: pre-line;
    display: inline;
    font-family: "LXGW WenKai", "Noto Serif SC", "Source Han Serif SC", "PingFang SC", serif;
}

.cursor {
    font-weight: 100;
    color: var(--color-primary, #3b82f6);
    animation: none;
}

.cursor-blink {
    animation: blink 1s step-end infinite;
}

@keyframes blink {
    0%, 100% { opacity: 1; }
    50% { opacity: 0; }
}
</style>
