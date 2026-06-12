import axios from '@/axios'

export function getJournalList(data) {
    return axios.post('/admin/journal/list', data)
}

export function createJournal(data) {
    return axios.post('/admin/journal/create', data)
}

export function updateJournal(data) {
    return axios.post('/admin/journal/update', data)
}

export function deleteJournal(id) {
    return axios.post('/admin/journal/delete', { id })
}

export function getJournalDetail(id) {
    return axios.post('/admin/journal/detail', { id })
}
