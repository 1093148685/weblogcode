<template>
    <canvas ref="canvasRef" class="global-canvas-bg"></canvas>
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
    if (!canvas) {
        console.error('Canvas element not found')
        return
    }
    
    ctx = canvas.getContext('2d')
    if (!ctx) {
        console.error('Failed to get 2D context')
        return
    }
    
    width = window.innerWidth
    height = window.innerHeight
    canvas.width = width
    canvas.height = height
    
    createParticles()
}

const createParticles = () => {
    particles = []
    const particleCount = Math.floor((width * height) / 12000)
    
    for (let i = 0; i < particleCount; i++) {
        particles.push(createParticle())
    }
}

const createParticle = () => {
    const size = Math.random() * 2 + 0.5
    return {
        x: Math.random() * width,
        y: Math.random() * height,
        size: size,
        speedX: (Math.random() - 0.5) * 0.3,
        speedY: (Math.random() - 0.5) * 0.3,
        opacity: Math.random() * 0.6 + 0.15,
        hue: Math.random() * 40 + 200,
    }
}

const getColors = () => {
    if (isDark.value) {
        return {
            bg1: '#030712',
            bg2: '#0f172a',
            particle: 'rgba(56, 189, 248, ',
            lineColor: 'rgba(56, 189, 248, ',
        }
    }
    return {
        bg1: '#f0f9ff',
        bg2: '#e0f2fe',
        particle: 'rgba(14, 165, 233, ',
        lineColor: 'rgba(14, 165, 233, ',
    }
}

const drawBackground = () => {
    const colors = getColors()
    const gradient = ctx.createLinearGradient(0, 0, width, height)
    gradient.addColorStop(0, colors.bg1)
    gradient.addColorStop(0.5, colors.bg2)
    gradient.addColorStop(1, colors.bg1)
    ctx.fillStyle = gradient
    ctx.fillRect(0, 0, width, height)
}

const drawParticles = () => {
    const colors = getColors()
    
    for (let i = 0; i < particles.length; i++) {
        const p = particles[i]
        
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
        
        for (let j = i + 1; j < particles.length; j++) {
            const p2 = particles[j]
            const dx = p.x - p2.x
            const dy = p.y - p2.y
            const distance = Math.sqrt(dx * dx + dy * dy)
            
            if (distance < 150) {
                ctx.beginPath()
                ctx.moveTo(p.x, p.y)
                ctx.lineTo(p2.x, p2.y)
                ctx.strokeStyle = colors.lineColor + ((150 - distance) / 150 * 0.08) + ')'
                ctx.lineWidth = 0.3
                ctx.stroke()
            }
        }
    }
}

const animate = () => {
    if (!ctx) return
    
    drawBackground()
    drawParticles()
    
    animationId = requestAnimationFrame(animate)
}

const handleResize = () => {
    width = window.innerWidth
    height = window.innerHeight
    if (canvasRef.value) {
        canvasRef.value.width = width
        canvasRef.value.height = height
        createParticles()
    }
}

let darkModeObserver = null

onMounted(() => {
    console.log('GlobalCanvasBackground mounted')
    checkDarkMode()
    initCanvas()
    animate()
    
    window.addEventListener('resize', handleResize)
    
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
    window.removeEventListener('resize', handleResize)
    if (darkModeObserver) {
        darkModeObserver.disconnect()
    }
})
</script>

<style scoped>
.global-canvas-bg {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    z-index: 0;
    pointer-events: none;
    background: linear-gradient(135deg, #e0f2fe 0%, #f0f9ff 100%);
}

html.dark .global-canvas-bg {
    background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
}
</style>
