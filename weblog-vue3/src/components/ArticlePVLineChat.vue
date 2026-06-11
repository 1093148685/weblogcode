<template>
    <div ref="chartRef" class="overflow-x-auto w-full h-60"></div>
</template>

<script setup>
import * as echarts from 'echarts'
import { ref, watch, onBeforeUnmount } from 'vue'

const props = defineProps({
    value: { type: Object, default: null }
})

const chartRef = ref(null)
let myChart = null

function initLineChat() {
    if (!chartRef.value) return
    const rootStyle = getComputedStyle(chartRef.value)
    const textColor = rootStyle.getPropertyValue('--admin-text-muted').trim() || '#94a3b8'
    const lineColor = 'rgba(148, 163, 184, 0.26)'
    const accent = rootStyle.getPropertyValue('--admin-accent').trim() || '#60a5fa'

    if (myChart) myChart.dispose()
    myChart = echarts.init(chartRef.value)

    const pvDates = props.value?.pvDates || []
    const pvCounts = props.value?.pvCounts || []

    myChart.setOption({
        xAxis: {
            type: 'category',
            data: pvDates,
            axisLabel: { color: textColor },
            axisLine: { lineStyle: { color: lineColor } },
            axisTick: { lineStyle: { color: lineColor } }
        },
        yAxis: {
            type: 'value',
            axisLabel: { color: textColor },
            splitLine: { lineStyle: { color: lineColor } }
        },
        series: [{
            data: pvCounts, type: 'line', smooth: true,
            symbolSize: 7, lineStyle: { color: accent, width: 3 },
            itemStyle: { color: accent }
        }]
    })
}

watch(() => props.value, () => initLineChat(), { deep: true })

onBeforeUnmount(() => {
    if (myChart) myChart.dispose()
})
</script>
