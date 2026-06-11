import axios from "@/axios";

export function subscribeByEmail(email) {
    return axios.post("/subscribe", { email })
}
