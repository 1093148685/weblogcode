import axios from '@/axios'

export function getJournalList(data) {
    return axios.post('/journal/list', data)
}

export function getJournalDetail(journalId) {
    return axios.post('/journal/detail', { journalId })
}

export function getJournalDates() {
    return axios.post('/journal/dates')
}
