import axios from "@/axios";

export function commentAdminLogin(data) {
    return axios.post("/comment-admin/login", data)
}

export function commentAdminVerify() {
    return axios.post("/comment-admin/verify")
}

export function commentAdminLogout() {
    return axios.post("/comment-admin/logout")
}

export function commentAdminInfo() {
    return axios.get("/comment-admin/info")
}

export function getCaptcha() {
    return axios.get("/comment/captcha")
}

export function verifySecret(data) {
    return axios.post("/comment/verify-secret", data)
}

export function resetSecret(data) {
    return axios.post("/comment-admin/reset-secret", data)
}
