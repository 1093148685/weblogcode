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

    const data = props.value.map(item => ({
        name: item.name,
        value: item.count
    }))

    const option = {
        tooltip: {
            trigger: 'item',
            formatter: '{b}: {c} ({d}%)'
        },
        legend: {
            orient: 'vertical',
            right: 10,
            top: 'center',
            textStyle: {
                fontSize: 12
            }
        },
        series: [
            {
                type: 'pie',
                radius: ['40%', '70%'],
                center: ['40%', '50%'],
                avoidLabelOverlap: false,
                itemStyle: {
                    borderRadius: 5,
                    borderColor: '#fff',
                    borderWidth: 2
                },
                label: {
                    show: false,
                    position: 'center'
                },
                emphasis: {
                    label: {
                        show: true,
                        fontSize: 14,
                        fontWeight: 'bold'
                    }
                },
                labelLine: {
                    show: false
                },
                data: data,
                color: [
                    '#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de',
                    '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc', '#91cc75'
                ]
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
