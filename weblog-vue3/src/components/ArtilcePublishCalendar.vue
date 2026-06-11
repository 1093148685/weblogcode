<template>
    <div ref="chartRef" class="overflow-x-auto w-full h-60"></div>
</template>

<script setup>
import * as echarts from 'echarts'
import { ref, watch, onBeforeUnmount } from 'vue'
import { format, subMonths } from 'date-fns'

const props = defineProps({
    value: { type: Object, default: null }
})

const chartRef = ref(null)
let myChart = null

const currentDate = new Date()
const sixMonthsAgo = subMonths(currentDate, 6)
const startDate = format(sixMonthsAgo, 'yyyy-MM-dd')
const endDate = format(currentDate, 'yyyy-MM-dd')

function initCalendar() {
    if (!chartRef.value) return
    const rootStyle = getComputedStyle(chartRef.value)
    const textColor = rootStyle.getPropertyValue('--admin-text-muted').trim() || '#94a3b8'
    const lineColor = 'rgba(148, 163, 184, 0.34)'
    const emptyColor = 'rgba(15, 23, 42, 0.62)'

    const myData = []
    const map = props.value || {}
    for (const key in map) {
        myData.push([key, map[key]])
    }

    if (myChart) myChart.dispose()
    myChart = echarts.init(chartRef.value)

    myChart.setOption({
        visualMap: { show: false, min: 0, max: 10, inRange: { color: [emptyColor, '#14532d', '#16a34a', '#4ade80'] } },
        calendar: {
            range: [startDate, endDate],
            itemStyle: { color: emptyColor, borderColor: lineColor, borderWidth: 1 },
            yearLabel: { color: textColor },
            monthLabel: { color: textColor },
            dayLabel: { color: textColor },
            splitLine: { lineStyle: { color: lineColor, width: 1 } }
        },
        series: { type: 'heatmap', coordinateSystem: 'calendar', data: myData }
    })
}

watch(() => props.value, () => initCalendar(), { deep: true })

onBeforeUnmount(() => {
    if (myChart) myChart.dispose()
})
</script>
