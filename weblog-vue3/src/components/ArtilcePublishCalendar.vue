<template>
    <!-- 日历热点图容器 -->
    <div id="calendar" class="overflow-x-auto w-full h-60"></div>
</template>

<script setup>
import * as echarts from 'echarts'
import { watch } from 'vue'
import { format, subMonths } from 'date-fns'

// 对外暴露的属性值
const props = defineProps({
    value: { // 属性值名称
        type: Object, // 类型为对象
        default: null // 默认为 null
    }
})

// 当前日期
const currentDate = new Date();
// 半年前
const sixMonthsAgo = subMonths(currentDate, 6)

// 格式化后的开始、结束日期
const startDate = format(sixMonthsAgo, 'yyyy-MM-dd')
const endDate = format(currentDate, 'yyyy-MM-dd')

// 日历热点数据
const myData = []

// 初始化日历热点图
function initCalendar() {
    var chartDom = document.getElementById('calendar');
    const rootStyle = getComputedStyle(chartDom)
    const textColor = rootStyle.getPropertyValue('--admin-text-muted').trim() || '#94a3b8'
    const lineColor = 'rgba(148, 163, 184, 0.34)'
    const emptyColor = 'rgba(15, 23, 42, 0.62)'
    // 将传入的数据设置到 myDate 数组中
    myData.length = 0
    let map = props.value
    for (let key in map) {
        myData.push([
            key,
            map[key]
        ]);
    }

    var myChart = echarts.init(chartDom, null, { width: 600 });
    var option;

    option = {
        visualMap: {
            show: false,
            min: 0,
            max: 10,
            inRange: {
                color: [emptyColor, '#14532d', '#16a34a', '#4ade80']
            }
        },
        calendar: { // 日历显示的范围：开始日期 - 结束日期
            range: [startDate, endDate],
            itemStyle: {
                color: emptyColor,
                borderColor: lineColor,
                borderWidth: 1
            },
            yearLabel: { color: textColor },
            monthLabel: { color: textColor },
            dayLabel: { color: textColor },
            splitLine: {
                lineStyle: {
                    color: lineColor,
                    width: 1
                }
            }
        },
        series: {
            type: 'heatmap',
            coordinateSystem: 'calendar',
            data: myData
        }, 
        gradientColor: [ // 自定义热点颜色，参考了 GitHub 代码提交的颜色
            emptyColor,
            '#40c463',
            '#30a14e',
            '#216e39',
        ]
    };

    option && myChart.setOption(option);
}

// 侦听属性, 监听 props.value 的变化，一旦 props.value 发生变化，就调用 initCalendar 初始化日历热点图
watch(() => props.value, () => initCalendar())

</script>
