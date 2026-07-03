<template>
    <div class="w-full bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 overflow-hidden">
        <div class="flex items-center gap-2 px-4 pt-4 pb-1">
            <svg class="icon w-4 h-4 text-sky-500" viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
                <path d="M880 184H712v-64c0-4.4-3.6-8-8-8h-56c-4.4 0-8 3.6-8 8v64H384v-64c0-4.4-3.6-8-8-8h-56c-4.4 0-8 3.6-8 8v64H144c-17.7 0-32 14.3-32 32v664c0 17.7 14.3 32 32 32h736c17.7 0 32-14.3 32-32V216c0-17.7-14.3-32-32-32z m-40 656H184V460h656v380zM184 332V256h128v48c0 4.4 3.6 8 8 8h56c4.4 0 8-3.6 8-8v-48h256v48c0 4.4 3.6 8 8 8h56c4.4 0 8-3.6 8-8v-48h128v76H184z" fill="currentColor"/>
            </svg>
            <span class="font-bold text-sm text-gray-700 dark:text-gray-300 uppercase">日历</span>
            <span v-if="selectedDate" class="ml-auto text-xs text-sky-500 cursor-pointer hover:text-sky-600" @click="clearFilter">清除</span>
        </div>
        <el-calendar v-model="calendarDate" class="journal-calendar">
            <template #date-cell="{ data }">
                <div class="calendar-day"
                    :class="{
                        'is-selected': isSelected(data.date),
                        'is-today': isToday(data.date),
                        'no-journal': journalDates.length > 0 && !hasJournal(data.date)
                    }"
                    @click="onDateClick(data.date)">
                    {{ data.day.split('-').pop().replace(/^0/, '') }}
                </div>
            </template>
        </el-calendar>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import moment from 'moment'
import { getJournalDates } from '@/api/frontend/journal'

const props = defineProps({
    selectedDate: { type: String, default: null }
})

const emit = defineEmits(['date-filter', 'date-clear'])

const calendarDate = ref(new Date())
const journalDates = ref([])

function fetchJournalDates() {
    getJournalDates().then(res => {
        if (res.success) {
            journalDates.value = res.data
        }
    })
}

onMounted(() => {
    fetchJournalDates()
})

function hasJournal(date) {
    const formatted = moment(date).format('YYYY-MM-DD')
    return journalDates.value.includes(formatted)
}

function onDateClick(date) {
    const formatted = moment(date).format('YYYY-MM-DD')
    // 如果点击的是已选中的日期，则清除筛选
    if (props.selectedDate === formatted) {
        emit('date-clear')
    } else {
        emit('date-filter', formatted)
    }
}

function isSelected(date) {
    return props.selectedDate === moment(date).format('YYYY-MM-DD')
}

function isToday(date) {
    return moment().format('YYYY-MM-DD') === moment(date).format('YYYY-MM-DD')
}

function clearFilter() {
    emit('date-clear')
}
</script>

<style scoped>
.journal-calendar :deep(.el-calendar__header) {
    padding: 4px 16px;
}
.journal-calendar :deep(.el-calendar__title) {
    font-size: 13px;
    color: var(--text-secondary, #64748b);
}
.journal-calendar :deep(.el-calendar__button-group) {
    display: flex;
    gap: 2px;
}
.journal-calendar :deep(.el-calendar__button-group .el-button) {
    padding: 2px 8px;
    font-size: 12px;
}
.journal-calendar :deep(.el-calendar-table) {
    padding: 0 4px 8px;
}
.journal-calendar :deep(.el-calendar-table th) {
    padding: 4px 0;
    font-size: 12px;
    font-weight: 400;
    color: var(--text-muted, #94a3b8);
}
.journal-calendar :deep(.el-calendar-table .el-calendar-day) {
    height: 30px;
    padding: 0;
}
.journal-calendar :deep(.el-calendar-table td) {
    border: none;
    padding: 1px;
}
.journal-calendar :deep(.el-calendar-table td.is-today .calendar-day) {
    color: var(--el-color-primary);
}
.calendar-day {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 30px;
    height: 30px;
    margin: 0 auto;
    border-radius: 50%;
    font-size: 13px;
    cursor: pointer;
    color: var(--text-body, #334155);
    transition: all 0.15s;
}
.dark .calendar-day {
    color: #cbd5e1;
}
.calendar-day:hover {
    background-color: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
}
.calendar-day.is-selected {
    background-color: var(--el-color-primary);
    color: #fff;
}
.calendar-day.is-today {
    font-weight: 700;
}
.calendar-day.no-journal {
    color: #cbd5e1;
}
.dark .calendar-day.no-journal {
    color: #475569;
}
</style>
