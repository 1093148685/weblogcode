import axios from "@/axios";

export function getStickerPacks() {
    return axios.get('/sticker/packs')
}

export function getStickerPack(id) {
    return axios.get(`/sticker/packs/${id}`)
}

export function searchGiphy(query, limit = 20, offset = 0) {
    return axios.get('/giphy/search', { params: { q: query, limit, offset } })
}

export function getTrendingGiphy(limit = 20, offset = 0) {
    return axios.get('/giphy/trending', { params: { limit, offset } })
}