import axios from "@/axios";

// 文章搜索
export function getArticleSearchPageList(data) {
    return axios.post("/search/article", data)
}


