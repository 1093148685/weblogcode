import axios from "@/axios";

export function createStickerPack(data) {
    return axios.post("/admin/sticker/pack/create", data)
}

export function getAllStickerPacks() {
    return axios.post("/admin/sticker/pack/list", {})
}

export function updateStickerPack(data) {
    return axios.post("/admin/sticker/pack/update", data)
}

export function deleteStickerPack(id) {
    return axios.post("/admin/sticker/pack/delete", {id})
}

export function uploadStickerZip(formData) {
    return axios.post("/admin/sticker/uploadZip", formData)
}

export function deleteSticker(id) {
    return axios.post("/admin/sticker/delete", {id})
}

export function setStickerCover(data) {
    return axios.post("/admin/sticker/setCover", data)
}
