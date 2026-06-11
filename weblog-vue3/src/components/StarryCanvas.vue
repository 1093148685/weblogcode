<template>
    <canvas ref="canvasRef" class="interactive-starry-canvas"></canvas>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const canvasRef = ref(null)
let ctx = null
let animationId = null
let stars = []
let isDarkMode = false
let mouseX = 0
let mouseY = 0
let particles = []
let bigDipper = null

const STAR_COLORS_NIGHT = [
    'rgba(255, 255, 255, 1)',
    'rgba(200, 220, 255, 1)',
    'rgba(255, 250, 220, 1)',
    'rgba(255, 200, 150, 1)',
    'rgba(150, 180, 255, 1)'
]

const STAR_COLORS_DAY = [
    'rgba(100, 150, 255, 0.6)',
    'rgba(150, 200, 255, 0.5)',
    'rgba(200, 220, 255, 0.4)'
]

class BigDipperStar {
    constructor(offsetX, offsetY, size = 4) {
        this.offsetX = offsetX
        this.offsetY = offsetY
        this.baseSize = size
        this.size = size
        this.twinklePhase = Math.random() * Math.PI * 2
        this.twinkleSpeed = 0.03
    }

    update(time, baseX, baseY) {
        this.twinklePhase += this.twinkleSpeed
        const twinkle = 0.7 + 0.3 * Math.sin(this.twinklePhase)
        
        const dx = mouseX - baseX
        const dy = mouseY - baseY
        const dist = Math.sqrt(dx * dx + dy * dy)
        const maxDist = 200
        
        if (dist < maxDist) {
            const force = (maxDist - dist) / maxDist
            this.size = this.baseSize + force * 2
        } else {
            this.size = this.baseSize * twinkle
        }
        
        return {
            x: baseX + this.offsetX,
            y: baseY + this.offsetY
        }
    }

    draw(x, y, opacity = 1) {
        ctx.beginPath()
        ctx.arc(x, y, this.size, 0, Math.PI * 2)
        ctx.fillStyle = `rgba(255, 255, 255, ${opacity})`
        ctx.fill()
        
        ctx.beginPath()
        ctx.arc(x, y, this.size * 3, 0, Math.PI * 2)
        ctx.fillStyle = `rgba(200, 220, 255, ${opacity * 0.15})`
        ctx.fill()
    }
}

class BigDipper {
    constructor(canvas) {
        this.canvas = canvas
        this.stars = []
        this.connections = []
        this.baseX = 0
        this.baseY = 0
        this.speedX = 0.1
        this.speedY = 0.02
        this.init()
    }

    init() {
        const scale = Math.min(this.canvas.width, this.canvas.height) / 1200
        const bigDipperPattern = [
            { dx: 0, dy: 0, size: 4.5 },
            { dx: 30 * scale, dy: -15 * scale, size: 3.5 },
            { dx: 55 * scale, dy: -5 * scale, size: 3.5 },
            { dx: 65 * scale, dy: 20 * scale, size: 3 },
            { dx: 50 * scale, dy: 35 * scale, size: 3 },
            { dx: 85 * scale, dy: 45 * scale, size: 4 },
            { dx: 120 * scale, dy: 35 * scale, size: 4.5 }
        ]
        
        bigDipperPattern.forEach(star => {
            this.stars.push(new BigDipperStar(star.dx, star.dy, star.size * scale * 1.2))
        })
        
        this.connections = [
            [0, 1], [1, 2], [2, 3], [3, 4], [4, 5], [5, 6]
        ]
        
        this.resetPosition()
    }

    resetPosition() {
        this.baseX = this.canvas.width * 0.7
        this.baseY = this.canvas.height * 0.15
    }

    update(time) {
        this.baseX += this.speedX
        this.baseY += this.speedY
        
        if (this.baseX > this.canvas.width + 200) {
            this.baseX = -200
        }
        if (this.baseY > this.canvas.height * 0.4) {
            this.baseY = -100
        }
    }

    draw() {
        const positions = this.stars.map((star, i) => {
            return star.update(0, this.baseX, this.baseY)
        })
        
        ctx.strokeStyle = 'rgba(200, 220, 255, 0.3)'
        ctx.lineWidth = 1
        ctx.setLineDash([5, 5])
        
        this.connections.forEach(([from, to]) => {
            ctx.beginPath()
            ctx.moveTo(positions[from].x, positions[from].y)
            ctx.lineTo(positions[to].x, positions[to].y)
            ctx.stroke()
        })
        
        ctx.setLineDash([])
        
        positions.forEach((pos, i) => {
            const twinkle = 0.7 + 0.3 * Math.sin(this.stars[i].twinklePhase)
            this.stars[i].draw(pos.x, pos.y, twinkle)
        })
    }
}

class Star {
    constructor(canvas) {
        this.canvas = canvas
        this.reset()
    }

    reset() {
        this.x = Math.random() * this.canvas.width
        this.y = Math.random() * this.canvas.height
        this.size = Math.random() * 2 + 0.5
        this.baseSize = this.size
        this.opacity = Math.random() * 0.5 + 0.5
        this.baseOpacity = this.opacity
        this.twinkleSpeed = Math.random() * 0.02 + 0.01
        this.twinklePhase = Math.random() * Math.PI * 2
        this.colors = isDarkMode ? STAR_COLORS_NIGHT : STAR_COLORS_DAY
        this.colorIndex = Math.floor(Math.random() * this.colors.length)
    }

    update(time) {
        this.twinklePhase += this.twinkleSpeed
        this.opacity = this.baseOpacity * (0.7 + 0.3 * Math.sin(this.twinklePhase))
        
        const dx = mouseX - this.x
        const dy = mouseY - this.y
        const dist = Math.sqrt(dx * dx + dy * dy)
        const maxDist = 150
        
        if (dist < maxDist) {
            const force = (maxDist - dist) / maxDist
            const angle = Math.atan2(dy, dx)
            this.x -= Math.cos(angle) * force * 2
            this.y -= Math.sin(angle) * force * 2
            this.size = this.baseSize + force * 1.5
            this.opacity = Math.min(1, this.baseOpacity + force * 0.5)
        } else {
            this.size = this.baseSize
        }
        
        if (this.x < 0) this.x = this.canvas.width
        if (this.x > this.canvas.width) this.x = 0
        if (this.y < 0) this.y = this.canvas.height
        if (this.y > this.canvas.height) this.y = 0
    }

    draw() {
        ctx.beginPath()
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2)
        ctx.fillStyle = this.colors[this.colorIndex].replace(/[\d.]+\)$/, `${this.opacity})`)
        ctx.fill()
        
        if (isDarkMode && this.size > 1.5) {
            ctx.beginPath()
            ctx.arc(this.x, this.y, this.size * 2, 0, Math.PI * 2)
            const glow = this.colors[this.colorIndex].replace(/[\d.]+\)$/, `${this.opacity * 0.2})`)
            ctx.fillStyle = glow
            ctx.fill()
        }
    }
}

class Particle {
    constructor(canvas) {
        this.canvas = canvas
        this.reset()
    }

    reset() {
        this.x = Math.random() * this.canvas.width
        this.y = Math.random() * this.canvas.height
        this.size = Math.random() * 3 + 1
        this.speedX = (Math.random() - 0.5) * 0.5
        this.speedY = isDarkMode ? Math.random() * 0.5 + 0.2 : (Math.random() - 0.5) * 0.3
        this.opacity = Math.random() * 0.3 + 0.1
        this.hue = isDarkMode ? (Math.random() * 60 + 220) : (Math.random() * 40 + 180)
    }

    update() {
        this.x += this.speedX
        this.y += this.speedY
        
        if (isDarkMode) {
            const dx = mouseX - this.x
            const dy = mouseY - this.y
            const dist = Math.sqrt(dx * dx + dy * dy)
            const maxDist = 200
            if (dist < maxDist) {
                const force = (maxDist - dist) / maxDist * 0.5
                this.x -= dx * force * 0.02
                this.y -= dy * force * 0.02
            }
        }
        
        if (this.x < 0 || this.x > this.canvas.width || 
            this.y < 0 || this.y > this.canvas.height) {
            this.reset()
            if (!isDarkMode) {
                this.y = 0
            }
        }
    }

    draw() {
        ctx.beginPath()
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2)
        if (isDarkMode) {
            ctx.fillStyle = `hsla(${this.hue}, 80%, 70%, ${this.opacity})`
        } else {
            ctx.fillStyle = `hsla(${this.hue}, 60%, 80%, ${this.opacity})`
        }
        ctx.fill()
    }
}

function initCanvas() {
    const canvas = canvasRef.value
    if (!canvas) return
    
    canvas.width = window.innerWidth
    canvas.height = window.innerHeight
    
    ctx = canvas.getContext('2d')
    
    const starCount = Math.min(200, Math.floor((canvas.width * canvas.height) / 8000))
    stars = []
    for (let i = 0; i < starCount; i++) {
        stars.push(new Star(canvas))
    }
    
    const particleCount = isDarkMode ? 50 : 30
    particles = []
    for (let i = 0; i < particleCount; i++) {
        particles.push(new Particle(canvas))
    }
    
    if (!bigDipper || isDarkMode) {
        bigDipper = new BigDipper(canvas)
    }
}

function drawDaySky() {
    const canvas = canvasRef.value
    if (!canvas || !ctx) return
    
    ctx.clearRect(0, 0, canvas.width, canvas.height)
    
    const gradient = ctx.createLinearGradient(0, 0, 0, canvas.height)
    gradient.addColorStop(0, 'rgba(135, 206, 235, 0.15)')
    gradient.addColorStop(0.5, 'rgba(255, 255, 255, 0.05)')
    gradient.addColorStop(1, 'rgba(200, 230, 255, 0.1)')
    ctx.fillStyle = gradient
    ctx.fillRect(0, 0, canvas.width, canvas.height)
}

function animate(time) {
    const canvas = canvasRef.value
    if (!canvas || !ctx) {
        animationId = requestAnimationFrame(animate)
        return
    }
    
    ctx.clearRect(0, 0, canvas.width, canvas.height)
    
    if (isDarkMode) {
        stars.forEach(star => {
            star.update(time)
            star.draw()
        })
        
        particles.forEach(particle => {
            particle.update()
            particle.draw()
        })
        
        if (bigDipper) {
            bigDipper.update(time)
            bigDipper.draw()
        }
    } else {
        drawDaySky()
        particles.forEach(particle => {
            particle.update()
            particle.draw()
        })
    }
    
    animationId = requestAnimationFrame(animate)
}

function handleMouseMove(e) {
    mouseX = e.clientX
    mouseY = e.clientY
}

function handleResize() {
    const canvas = canvasRef.value
    if (!canvas) return
    
    canvas.width = window.innerWidth
    canvas.height = window.innerHeight
    
    stars.forEach(star => {
        if (star.x > canvas.width || star.y > canvas.height) {
            star.reset()
        }
    })
    
    if (bigDipper) {
        bigDipper.baseX = canvas.width * 0.7
        bigDipper.baseY = canvas.height * 0.15
    }
}

function checkDarkMode() {
    const isDark = document.documentElement.classList.contains('dark')
    if (isDark !== isDarkMode) {
        isDarkMode = isDark
        stars.forEach(star => {
            star.colors = isDarkMode ? STAR_COLORS_NIGHT : STAR_COLORS_DAY
        })
        particles.forEach(particle => {
            particle.reset()
        })
        if (isDarkMode && !bigDipper) {
            bigDipper = new BigDipper(canvasRef.value)
        }
    }
}

let resizeObserver = null

onMounted(() => {
    checkDarkMode()
    initCanvas()
    animate(0)
    
    window.addEventListener('mousemove', handleMouseMove)
    window.addEventListener('resize', handleResize)
    
    resizeObserver = new MutationObserver(checkDarkMode)
    resizeObserver.observe(document.documentElement, { 
        attributes: true, 
        attributeFilter: ['class'] 
    })
})

onUnmounted(() => {
    if (animationId) {
        cancelAnimationFrame(animationId)
    }
    window.removeEventListener('mousemove', handleMouseMove)
    window.removeEventListener('resize', handleResize)
    if (resizeObserver) {
        resizeObserver.disconnect()
    }
})
</script>

<style scoped>
.interactive-starry-canvas {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 0;
}
</style>
