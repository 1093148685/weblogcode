import axios from "@/axios";

export function getMessageWallComments(routerUrl = "/message-wall") {
    return axios.post("/comment/list", { routerUrl })
}

export function publishMessageWallComment(data, routerUrl = "/message-wall") {
    return axios.post("/comment/publish", { ...data, routerUrl })
}

export function sendFlower(commentId) {
    return axios.post("/comment/flower", { commentId })
}

export function cancelFlower(commentId) {
    return axios.post("/comment/unflower", { commentId })
}

export function getFlowerStatus(commentIds) {
    return axios.post("/comment/flower/status", { commentIds })
}
