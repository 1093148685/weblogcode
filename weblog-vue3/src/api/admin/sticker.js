import axios from "@/axios";

export function createStickerPack(data) {
    return axios.post('/admin/sticker/packs', data)
}

export function getAllStickerPacks() {
    return axios.get('/admin/sticker/packs')
}

export function updateStickerPack(id, data) {
    return axios.put(`/admin/sticker/packs/${id}`, data)
}

export function deleteStickerPack(id) {
    return axios.delete(`/admin/sticker/packs/${id}`)
}

export function uploadStickerZip(packId, file) {
    const formData = new FormData()
    formData.append('file', file)
    return axios.post(`/admin/sticker/packs/${packId}/upload`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
    })
}

export function deleteSticker(id) {
    return axios.delete(`/admin/sticker/stickers/${id}`)
}

export function setStickerCover(packId, stickerId) {
    return axios.post(`/admin/sticker/packs/${packId}/cover/${stickerId}`)
}