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
                fontSize: 11
            }
        },
        yAxis: {
            type: 'category',
            data: data.map(item => item.name),
            axisLabel: {
                fontSize: 11,
                width: 60,
                overflow: 'truncate'
            }
        },
        series: [
            {
                name: '文章数',
                type: 'bar',
                data: data.map(item => item.count),
                itemStyle: {
                    color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
                        { offset: 0, color: '#73c0de' },
                        { offset: 1, color: '#5470c6' }
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
