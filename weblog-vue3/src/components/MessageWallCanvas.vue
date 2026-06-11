<template>
    <div class="message-wall-canvas-container relative overflow-hidden">
        <canvas ref="canvasRef" class="absolute inset-0 w-full h-full"></canvas>
        <div class="relative z-10">
            <slot></slot>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'

const canvasRef = ref(null)
let animationId = null
let ctx = null
let particles = []
let width = 0
let height = 0

const isDark = ref(false)

const checkDarkMode = () => {
    isDark.value = document.documentElement.classList.contains('dark')
}

const initCanvas = () => {
    const canvas = canvasRef.value
    if (!canvas) return
    
    ctx = canvas.getContext('2d')
    width = canvas.width = canvas.offsetWidth
    height = canvas.height = canvas.offsetHeight
    
    createParticles()
}

const createParticles = () => {
    particles = []
    const particleCount = Math.floor((width * height) / 15000)
    
    for (let i = 0; i < particleCount; i++) {
        particles.push(createParticle())
    }
}

const createParticle = () => {
    const size = Math.random() * 3 + 1
    return {
        x: Math.random() * width,
        y: Math.random() * height,
        size: size,
        speedX: (Math.random() - 0.5) * 0.5,
        speedY: (Math.random() - 0.5) * 0.5,
        opacity: Math.random() * 0.5 + 0.2,
        hue: Math.random() * 60 + 180,
    }
}

const getColors = () => {
    if (isDark.value) {
        return {
            bg1: '#0f172a',
            bg2: '#1e293b',
            particle: 'rgba(56, 189, 248, ',
        }
    }
    return {
        bg1: '#f0f9ff',
        bg2: '#e0f2fe',
        particle: 'rgba(14, 165, 233, ',
    }
}

const drawBackground = () => {
    const colors = getColors()
    const gradient = ctx.createLinearGradient(0, 0, width, height)
    gradient.addColorStop(0, colors.bg1)
    gradient.addColorStop(1, colors.bg2)
    ctx.fillStyle = gradient
    ctx.fillRect(0, 0, width, height)
}

const drawParticles = () => {
    const colors = getColors()
    
    particles.forEach((p, i) => {
        p.x += p.speedX
        p.y += p.speedY
        
        if (p.x < 0) p.x = width
        if (p.x > width) p.x = 0
        if (p.y < 0) p.y = height
        if (p.y > height) p.y = 0
        
        ctx.beginPath()
        ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2)
        ctx.fillStyle = colors.particle + p.opacity + ')'
        ctx.fill()
        
        if (i % 3 === 0) {
            ctx.beginPath()
            ctx.moveTo(p.x, p.y)
            const nextParticle = particles[(i + 1) % particles.length]
            ctx.lineTo(nextParticle.x, nextParticle.y)
            ctx.strokeStyle = colors.particle + (p.opacity * 0.1) + ')'
            ctx.lineWidth = 0.5
            ctx.stroke()
        }
    })
}

const animate = () => {
    if (!ctx) return
    
    drawBackground()
    drawParticles()
    
    animationId = requestAnimationFrame(animate)
}

const handleResize = () => {
    width = canvasRef.value.offsetWidth
    height = canvasRef.value.offsetHeight
    canvasRef.value.width = width
    canvasRef.value.height = height
    createParticles()
}

let resizeObserver = null
let darkModeObserver = null

onMounted(() => {
    checkDarkMode()
    initCanvas()
    animate()
    
    resizeObserver = new ResizeObserver(handleResize)
    resizeObserver.observe(canvasRef.value.parentElement)
    
    darkModeObserver = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.attributeName === 'class') {
                checkDarkMode()
            }
        })
    })
    darkModeObserver.observe(document.documentElement, { attributes: true })
})

onBeforeUnmount(() => {
    if (animationId) {
        cancelAnimationFrame(animationId)
    }
    if (resizeObserver) {
        resizeObserver.disconnect()
    }
    if (darkModeObserver) {
        darkModeObserver.disconnect()
    }
})
</script>

<style scoped>
.message-wall-canvas-container {
    min-height: 60vh;
}
</style>
