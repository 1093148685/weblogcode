<template>
    <div
        ref="containerRef"
        class="spoiler-text cursor-pointer select-none"
        :class="{ 'is-revealing': isRevealing, 'is-revealed': isRevealed }"
        :style="{ '--reveal-duration': revealDuration + 'ms' }"
        @click="handleClick"
        @keydown.enter.prevent="handleClick"
        @keydown.space.prevent="handleClick"
        tabindex="0"
        role="button"
        :aria-pressed="isRevealed"
        :aria-label="isRevealed ? undefined : '点击显示隐藏内容'"
    >
        <slot></slot>
        <canvas 
            ref="canvasRef"
            class="spoiler-canvas"
        ></canvas>
    </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'

const props = defineProps({
    scale: { type: Number, default: 1 },
    density: { type: Number, default: 8 },
    minVelocity: { type: Number, default: 0.01 },
    maxVelocity: { type: Number, default: 0.05 },
    particleLifetime: { type: Number, default: 120 },
    revealDuration: { type: Number, default: 300 },
    spawnStopDelay: { type: Number, default: 0 }
})

const containerRef = ref(null)
const canvasRef = ref(null)

const isRevealing = ref(false)
const isRevealed = ref(false)

let particles = []
let animationFrameId = null
let isSpawning = true
let textColor = '#000000'
let dpr = 1

const particleSizes = [
    { width: 1, height: 1 },
    { width: 1, height: 2 },
    { width: 2, height: 1 },
    { width: 2, height: 2 }
]

const createParticle = (width, height) => {
    const sizeTemplate = particleSizes[Math.floor(Math.random() * particleSizes.length)]
    const scaledSize = props.scale

    const particleWidth = sizeTemplate.width * scaledSize
    const particleHeight = sizeTemplate.height * scaledSize

    const padding = 2
    const maxX = width - particleWidth - padding
    const maxY = height - particleHeight - padding

    const x = padding + Math.random() * Math.max(0, maxX - padding)
    const y = padding + Math.random() * Math.max(0, maxY - padding)

    const speed = Math.random() * (props.maxVelocity - props.minVelocity) + props.minVelocity
    const angle = Math.random() * Math.PI * 2
    const vx = Math.cos(angle) * speed
    const vy = Math.sin(angle) * speed

    const lifetimeVariation = 0.5
    const minLifetime = props.particleLifetime * (1 - lifetimeVariation)
    const maxLifetime = props.particleLifetime * (1 + lifetimeVariation)
    const lifetime = Math.random() * (maxLifetime - minLifetime) + minLifetime

    const maxAlpha = Math.random() < 0.5 ? 1.0 : 0.3 + Math.random() * 0.3

    return {
        x, y, vx, vy,
        width: particleWidth,
        height: particleHeight,
        life: lifetime,
        maxLife: lifetime,
        alpha: 0,
        maxAlpha
    }
}

const setupSpoiler = () => {
    if (!canvasRef.value || !containerRef.value) return

    const canvas = canvasRef.value
    const container = containerRef.value

    const rect = container.getBoundingClientRect()
    if (rect.width === 0 || rect.height === 0) return

    dpr = window.devicePixelRatio || 1
    const width = rect.width
    const height = rect.height

    canvas.width = width * dpr
    canvas.height = height * dpr
    canvas.style.width = `${width}px`
    canvas.style.height = `${height}px`
    canvas.style.left = '0px'
    canvas.style.top = '0px'

    const ctx = canvas.getContext('2d', { alpha: true })
    if (!ctx) {
        console.error('Failed to get 2D context')
        return
    }

    ctx.scale(dpr, dpr)

    const computedStyle = window.getComputedStyle(container.parentElement)
    textColor = computedStyle.color || '#000000'

    const area = width * height
    const targetCount = Math.ceil((area / 100) * props.density)

    particles = []
    for (let i = 0; i < targetCount; i++) {
        const particle = createParticle(width, height)
        particle.life = Math.random() * particle.maxLife
        particles.push(particle)
    }

    isSpawning = true
    animate()
}

const updateAndDrawParticles = () => {
    const canvas = canvasRef.value
    if (!canvas) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const width = canvas.width / dpr
    const height = canvas.height / dpr

    ctx.clearRect(0, 0, width, height)
    ctx.fillStyle = textColor

    for (let i = particles.length - 1; i >= 0; i--) {
        const p = particles[i]

        p.x += p.vx
        p.y += p.vy
        p.life--

        const fadeInDuration = p.maxLife * 0.2
        const fadeOutDuration = p.maxLife * 0.2

        if (p.life > p.maxLife - fadeInDuration) {
            const fadeProgress = (p.maxLife - p.life) / fadeInDuration
            p.alpha = fadeProgress * p.maxAlpha
        } else if (p.life < fadeOutDuration) {
            const fadeProgress = p.life / fadeOutDuration
            p.alpha = fadeProgress * p.maxAlpha
        } else {
            p.alpha = p.maxAlpha
        }

        const margin = Math.max(width, height) * 0.5
        if (p.life <= 0 || p.x < -margin || p.x > width + margin || p.y < -margin || p.y > height + margin) {
            particles.splice(i, 1)
        } else {
            ctx.globalAlpha = p.alpha
            ctx.fillRect(Math.round(p.x), Math.round(p.y), Math.ceil(p.width), Math.ceil(p.height))
        }
    }

    ctx.globalAlpha = 1

    if (isSpawning && particles.length < Math.ceil((width * height / 100) * props.density)) {
        if (particles.length < 1000) {
            particles.push(createParticle(width, height))
        }
    }
}

const animate = () => {
    if (isRevealed.value) return

    updateAndDrawParticles()

    animationFrameId = requestAnimationFrame(animate)
}

const handleClick = () => {
    if (isRevealed.value || isRevealing.value) return

    isRevealing.value = true

    setTimeout(() => {
        isSpawning = false
    }, props.spawnStopDelay)

    const checkParticles = () => {
        if (particles.length === 0) {
            isRevealed.value = true
            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId)
                animationFrameId = null
            }
        } else {
            requestAnimationFrame(checkParticles)
        }
    }
    
    setTimeout(checkParticles, 100)
}

const cleanup = () => {
    if (animationFrameId) {
        cancelAnimationFrame(animationFrameId)
        animationFrameId = null
    }
    particles = []
}

let resizeObserver = null
let setupDebounceTimer = null

const handleSizeChange = () => {
    if (setupDebounceTimer) clearTimeout(setupDebounceTimer)
    setupDebounceTimer = setTimeout(() => {
        setupSpoiler()
        setupDebounceTimer = null
    }, 50)
}

onMounted(() => {
    nextTick(() => {
        setTimeout(() => setupSpoiler(), 100)

        resizeObserver = new ResizeObserver(() => {
            if (!isRevealed.value) {
                handleSizeChange()
            }
        })
        if (containerRef.value) {
            resizeObserver.observe(containerRef.value)
        }
    })
})

onBeforeUnmount(() => {
    cleanup()
    if (resizeObserver) {
        resizeObserver.disconnect()
    }
    if (setupDebounceTimer) clearTimeout(setupDebounceTimer)
})
</script>

<style scoped>
.spoiler-text {
    position: relative;
    display: inline-block;
    color: transparent;
    text-shadow: none;
    z-index: 5;
}

.spoiler-canvas {
    position: absolute;
    pointer-events: none;
    z-index: 5;
}

.spoiler-text.is-revealing {
    animation: spoiler-fade-in var(--reveal-duration, 300ms) ease-out forwards;
}

.spoiler-text.is-revealed {
    color: inherit;
    cursor: inherit;
    user-select: text;
}

@keyframes spoiler-fade-in {
    from {
        color: transparent;
    }
    to {
        color: inherit;
    }
}
</style>
