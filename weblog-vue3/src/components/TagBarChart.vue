<template>
    <div ref="chartRef" class="w-full h-60"></div>
</template>

<script setup>
import * as echarts from 'echarts'
import { ref, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'

const props = defineProps({
    value: {
        type: Array,
        default: () => []
    }
})

const chartRef = ref(null)
let myChart = null

function initChart() {
    if (!props.value || props.value.length === 0) return
    if (!chartRef.value) return

    // Dispose old chart instance if exists
    if (myChart) {
        myChart.dispose()
    }

    myChart = echarts.init(chartRef.value)
    const rootStyle = getComputedStyle(chartRef.value)
    const textColor = rootStyle.getPropertyValue('--admin-text-muted').trim() || '#94a3b8'
    const lineColor = 'rgba(148, 163, 184, 0.26)'
    const accent = rootStyle.getPropertyValue('--admin-accent').trim() || '#60a5fa'
    const accent2 = rootStyle.getPropertyValue('--admin-accent-2').trim() || '#22d3ee'

    const data = [...props.value].reverse()

    const option = {
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'shadow'
            }
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true
        },
        xAxis: {
            type: 'value',
            axisLabel: {
                fontSize: 11,
                color: textColor
            },
            axisLine: { lineStyle: { color: lineColor } },
            splitLine: { lineStyle: { color: lineColor } }
        },
        yAxis: {
            type: 'category',
            data: data.map(item => item.name),
            axisLabel: {
                fontSize: 11,
                width: 60,
                overflow: 'truncate',
                color: textColor
            },
            axisLine: { lineStyle: { color: lineColor } },
            axisTick: { lineStyle: { color: lineColor } }
        },
        series: [
            {
                name: '文章数',
                type: 'bar',
                data: data.map(item => item.count),
                itemStyle: {
                    color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
                        { offset: 0, color: accent2 },
                        { offset: 1, color: accent }
                    ])
                },
                barWidth: '60%'
            }
        ]
    }

    myChart.setOption(option)
}

// Handle window resize
function handleResize() {
    if (myChart) {
        myChart.resize()
    }
}

onMounted(() => {
    nextTick(() => {
        initChart()
    })
    window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
    window.removeEventListener('resize', handleResize)
    if (myChart) {
        myChart.dispose()
        myChart = null
    }
})

watch(() => props.value, () => {
    nextTick(() => initChart())
}, { deep: true })
</script>
