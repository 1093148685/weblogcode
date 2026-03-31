<template>
    <div class="spoiler-wrapper relative inline-block" ref="wrapperRef">
        <!-- 模糊内容层 -->
        <div class="blurred-content rounded-xl" :class="{ 'revealed': isRevealed }">
            <slot></slot>
        </div>
        
        <!-- 粒子遮罩层 -->
        <canvas 
            v-if="!isRevealed && !isExpired && !isReset"
            ref="canvasRef"
            class="spoiler-canvas absolute pointer-events-none z-35 rounded-xl"
            :class="{ 'dissolving': isDissolving }"
        ></canvas>

        <!-- 点击区域（透明按钮触发验证） -->
        <div 
            v-if="!isRevealed && !isExpired && !isReset"
            class="spoiler-trigger absolute inset-0 cursor-pointer z-20 rounded-xl"
            @click="handleClick"
        ></div>

        <!-- 已过期 -->
        <div v-if="isExpired" class="absolute inset-0 bg-gray-500/60 backdrop-blur-sm flex items-center justify-center rounded-xl">
            <span class="text-white/80 text-xs"><i class="fas fa-clock mr-1.5"></i>已过期</span>
        </div>

        <!-- 已重置 -->
        <div v-if="isReset" class="absolute inset-0 bg-red-500/60 backdrop-blur-sm flex items-center justify-center rounded-xl">
            <span class="text-white/80 text-xs"><i class="fas fa-ban mr-1.5"></i>已重置</span>
        </div>
    </div>
</template>

<script setup>
/**
 * SpoilerContent - 管理员私密内容组件
 * 
 * 特点：
 * 1. 五颜六色的粒子效果遮罩
 * 2. 点击需要密钥+验证码验证
 * 3. 验证成功后粒子消散显示内容
 */
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'

const props = defineProps({
    commentId: { type: Number, required: true },
    isExpired: { type: Boolean, default: false },
    isReset: { type: Boolean, default: false }
})

const emit = defineEmits(['verify-start', 'reveal-complete'])

const wrapperRef = ref(null)
const canvasRef = ref(null)

const isRevealed = ref(false)
const isDissolving = ref(false)

let particles = []
let animationFrameId = null
let dpr = 1

/** 五颜六色的粒子颜色池 */
const rainbowColors = [
    '#ff6b6b', // 红色
    '#feca57', // 黄色
    '#48dbfb', // 蓝色
    '#ff9ff3', // 粉色
    '#54a0ff', // 天蓝色
    '#5f27cd', // 紫色
    '#00d2d3', // 青色
    '#ff9f43', // 橙色
    '#10ac84', // 绿色
    '#ee5a24', // 橙红
    '#9b59b6', // 紫罗兰
    '#f8b739', // 金色
    '#2ecc71', // 翠绿
    '#e74c3c', // 珊瑚红
    '#3498db', // 浅蓝
    '#e91e63', // 品红
]

/** 粒子尺寸 */
const particleSizes = [
    { width: 2, height: 2 },
    { width: 3, height: 3 },
    { width: 2, height: 3 },
    { width: 3, height: 2 },
]

const createParticle = (width, height) => {
    const sizeTemplate = particleSizes[Math.floor(Math.random() * particleSizes.length)]
    const scale = 1 + Math.random() * 1.5 // 1-2.5倍大小变化
    
    const particleWidth = sizeTemplate.width * scale
    const particleHeight = sizeTemplate.height * scale

    const padding = 2
    const maxX = width - particleWidth - padding
    const maxY = height - particleHeight - padding

    const x = padding + Math.random() * Math.max(0, maxX - padding)
    const y = padding + Math.random() * Math.max(0, maxY - padding)

    // 随机速度
    const speed = 0.02 + Math.random() * 0.04
    const angle = Math.random() * Math.PI * 2
    const vx = Math.cos(angle) * speed
    const vy = Math.sin(angle) * speed

    // 随机生命周期
    const lifetime = 80 + Math.random() * 80 // 80-160帧
    const maxAlpha = 0.7 + Math.random() * 0.3 // 0.7-1.0

    // 随机颜色
    const color = rainbowColors[Math.floor(Math.random() * rainbowColors.length)]

    return {
        x, y, vx, vy,
        width: particleWidth,
        height: particleHeight,
        life: lifetime,
        maxLife: lifetime,
        alpha: 0,
        maxAlpha,
        color
    }
}

const initParticles = () => {
    if (!canvasRef.value || !wrapperRef.value) return

    const canvas = canvasRef.value
    const wrapper = wrapperRef.value
    const rect = wrapper.getBoundingClientRect()
    
    if (rect.width === 0 || rect.height === 0) return

    dpr = window.devicePixelRatio || 1
    const width = rect.width
    const height = rect.height

    canvas.width = width * dpr
    canvas.height = height * dpr
    canvas.style.width = `${width}px`
    canvas.style.height = `${height}px`
    canvas.style.left = '0'
    canvas.style.top = '0'

    const ctx = canvas.getContext('2d')
    ctx.scale(dpr, dpr)

    // 计算粒子数量
    const area = width * height
    const targetCount = Math.ceil((area / 100) * 15) // 高密度

    particles = []
    for (let i = 0; i < targetCount; i++) {
        const particle = createParticle(width, height)
        particle.life = Math.random() * particle.maxLife
        particles.push(particle)
    }

    animate()
}

const updateAndDrawParticles = () => {
    const canvas = canvasRef.value
    if (!canvas) return

    const ctx = canvas.getContext('2d')
    const width = canvas.width / dpr
    const height = canvas.height / dpr

    ctx.clearRect(0, 0, width, height)

    for (let i = particles.length - 1; i >= 0; i--) {
        const p = particles[i]

        p.x += p.vx
        p.y += p.vy
        p.life--

        // 淡入淡出
        const fadeInDuration = p.maxLife * 0.15
        const fadeOutDuration = p.maxLife * 0.15

        if (p.life > p.maxLife - fadeInDuration) {
            const fadeProgress = (p.maxLife - p.life) / fadeInDuration
            p.alpha = fadeProgress * p.maxAlpha
        } else if (p.life < fadeOutDuration) {
            const fadeProgress = p.life / fadeOutDuration
            p.alpha = fadeProgress * p.maxAlpha
        } else {
            p.alpha = p.maxAlpha
        }

        // 边界检测
        const margin = Math.max(width, height) * 0.5
        if (p.life <= 0 || p.x < -margin || p.x > width + margin || p.y < -margin || p.y > height + margin) {
            particles.splice(i, 1)
        } else {
            ctx.globalAlpha = p.alpha
            ctx.fillStyle = p.color
            ctx.fillRect(Math.round(p.x), Math.round(p.y), Math.ceil(p.width), Math.ceil(p.height))
        }
    }

    ctx.globalAlpha = 1

    // 补充粒子
    if (particles.length < Math.ceil((width * height / 100) * 15)) {
        if (particles.length < 1500) {
            particles.push(createParticle(width, height))
        }
    }
}

const animate = () => {
    if (isRevealed.value) return

    updateAndDrawParticles()

    animationFrameId = requestAnimationFrame(animate)
}

const cleanup = () => {
    if (animationFrameId) {
        cancelAnimationFrame(animationFrameId)
        animationFrameId = null
    }
    particles = []
}

const handleClick = () => {
    if (!isRevealed.value && !props.isExpired && !props.isReset) {
        emit('verify-start', props.commentId)
    }
}

const reveal = () => {
    isDissolving.value = true
    
    // 等待一段时间让粒子继续动画，然后完全消失
    setTimeout(() => {
        cleanup()
        isRevealed.value = true
        isDissolving.value = false
        emit('reveal-complete')
    }, 1500)
}

const reset = () => {
    isRevealed.value = false
    isDissolving.value = false
    nextTick(() => initParticles())
}

let resizeObserver = null

onMounted(() => {
    nextTick(() => {
        setTimeout(() => initParticles(), 100)

        resizeObserver = new ResizeObserver(() => {
            if (!isRevealed.value) {
                nextTick(() => initParticles())
            }
        })
        if (wrapperRef.value) {
            resizeObserver.observe(wrapperRef.value)
        }
    })
})

onBeforeUnmount(() => {
    cleanup()
    if (resizeObserver) {
        resizeObserver.disconnect()
    }
})

defineExpose({ reveal, reset })
</script>

<style scoped>
.spoiler-wrapper {
    display: inline-block;
}

.blurred-content {
    filter: blur(8px) saturate(0.3);
    user-select: none;
    pointer-events: none;
    transition: filter 1.5s cubic-bezier(0.4, 0, 0.2, 1);
}

.blurred-content.revealed {
    filter: blur(0) saturate(1);
    user-select: auto;
    pointer-events: auto;
}

.spoiler-canvas {
    position: absolute;
    inset: 0;
}

.spoiler-trigger {
    /* 透明触发区域 */
}

.spoiler-canvas.dissolving {
    animation: canvas-fade-out 1.5s ease-out forwards;
}

@keyframes canvas-fade-out {
    0% { opacity: 1; }
    100% { opacity: 0; }
}
</style>
