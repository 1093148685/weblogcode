<template>
    <!-- PV 折线图容器 -->
    <div id="lineChat" class="overflow-x-auto w-full h-60"></div>
</template>

<script setup>
import * as echarts from 'echarts'
import { watch } from 'vue'

// 对外暴露的属性值
const props = defineProps({
    value: { // 属性值名称
        type: Object, // 类型为对象
        default: null // 默认为 null
    }
})

// 初始化折线图
function initLineChat() {
    var chartDom = document.getElementById('lineChat');
    const rootStyle = getComputedStyle(chartDom)
    const textColor = rootStyle.getPropertyValue('--admin-text-muted').trim() || '#94a3b8'
    const lineColor = 'rgba(148, 163, 184, 0.26)'
    const accent = rootStyle.getPropertyValue('--admin-accent').trim() || '#60a5fa'
    var myChart = echarts.init(chartDom, null, { width: 600 });
    var option;

    // 从 props.value 中获取日期集合和 pv 访问量集合
    const pvDates = props.value.pvDates
    const pvCounts = props.value.pvCounts

    option = {
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
        series: [
            {
                data: pvCounts,
                type: 'line',
                smooth: true,
                symbolSize: 7,
                lineStyle: { color: accent, width: 3 },
                itemStyle: { color: accent }
            }
        ]
    };

    option && myChart.setOption(option);
}

// 侦听属性, 监听 props.value 的变化，一旦 props.value 发生变化，就调用 initLineChat 初始化折线图
watch(() => props.value, () => initLineChat())
</script>
