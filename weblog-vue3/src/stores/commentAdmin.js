import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useCommentAdminStore = defineStore('commentAdmin', () => {
  const isLoggedIn = ref(false)
  const adminInfo = ref(null)
  const token = ref(null)

  return { isLoggedIn, adminInfo, token }
},
{
  persist: true,
}
)
