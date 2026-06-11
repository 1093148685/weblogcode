import axios from "@/axios";

// 获取评论分页数据
export function getCommentPageList(data) {
    return axios.post("/admin/comment/list", data)
}

// 删除评论
export function deleteComment(id) {
    return axios.post("/admin/comment/delete", {id})
}

// 审核评论
export function examineComment(data) {
    return axios.post("/admin/comment/examine", data)
}

// 获取私密评论分页数据
export function getSecretCommentList(data) {
    return axios.post("/admin/comment/secret/list", data)
}

// 重置私密评论
export function resetSecretComment(data) {
    return axios.post("/admin/comment/secret/reset", data)
}

// 批量删除评论
export function batchDeleteComment(ids) {
    return axios.post("/admin/comment/batch/delete", { ids })
}

