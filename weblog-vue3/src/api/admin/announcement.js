import axios from "@/axios";

// 获取公告
export function getAnnouncement() {
    return axios.get("/announcement")
}

// 获取公告（管理员）
export function getAnnouncementAdmin() {
    return axios.get("/admin/announcement")
}

// 查询公告列表
export function getAnnouncementList(data) {
    return axios.post("/admin/announcement/list", data)
}

// 删除公告
export function deleteAnnouncement(id) {
    return axios.delete("/admin/announcement/" + id)
}

// 创建公告
export function createAnnouncement(data) {
    return axios.post("/admin/announcement", data)
}

// 更新公告
export function updateAnnouncement(data) {
    return axios.put("/admin/announcement", data)
}
