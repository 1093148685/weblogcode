import axios from "@/axios";

export function getMessageWallComments() {
    return axios.post("/comment/list", { routerUrl: "/message-wall" })
}

export function publishMessageWallComment(data) {
    return axios.post("/comment/publish", { ...data, routerUrl: "/message-wall" })
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