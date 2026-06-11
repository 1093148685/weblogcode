<template>
    <div>
        <!-- 搜索栏 -->
        <el-card shadow="never" class="mb-5">
            <div class="flex items-center flex-wrap gap-4">
                <el-input v-model="searchForm.nickname" placeholder="搜索昵称" style="width: 200px" clearable />
                <el-date-picker v-model="searchForm.dateRange" type="daterange" range-separator="至" start-placeholder="开始日期"
                    end-placeholder="结束日期" value-format="YYYY-MM-DD" style="width: 240px" />
                <el-button type="primary" :icon="Search" @click="search">搜索</el-button>
                <el-button :icon="RefreshRight" @click="reset">重置</el-button>
            </div>
        </el-card>

        <!-- 列表 -->
        <el-card shadow="never">
            <el-table :data="tableData" border stripe style="width: 100%" v-loading="loading" table-layout="auto">
                <el-table-column prop="id" label="ID" width="80" />
                <el-table-column prop="nickname" label="昵称" width="140">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <span>{{ row.nickname }}</span>
                            <el-tag v-if="row.isAdmin" size="small" type="warning">
                                <i class="fas fa-crown mr-1"></i>管理员
                            </el-tag>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="content" label="内容" show-overflow-tooltip />
                <el-table-column label="图片" width="100">
                    <template #default="{ row }">
                        <div v-if="row.images && row.images.length > 0" class="flex items-center gap-1">
                            <el-image 
                                v-if="row.imagesArray && row.imagesArray[0]" 
                                :src="row.imagesArray[0]" 
                                :preview-src-list="row.imagesArray"
                                :preview-teleported="true"
                                fit="cover"
                                class="w-10 h-10 rounded border cursor-pointer"
                                :initial-index="0"
                            />
                            <span v-if="row.imagesArray && row.imagesArray.length > 1" class="text-xs text-gray-500">
                                +{{ row.imagesArray.length - 1 }}
                            </span>
                        </div>
                        <span v-else class="text-gray-400 text-xs">无</span>
                    </template>
                </el-table-column>
                <el-table-column label="私密内容" width="100">
                    <template #default="{ row }">
                        <el-tag v-if="row.isSecret" type="warning" size="small">是</el-tag>
                        <el-tag v-else type="info" size="small">否</el-tag>
                    </template>
                </el-table-column>
                <el-table-column label="状态" width="120">
                    <template #default="{ row }">
                        <el-tag v-if="row.isExpired" type="info" size="small">已过期</el-tag>
                        <el-tag v-else-if="row.isReset" type="danger" size="small">已重置</el-tag>
                        <el-tag v-else type="success" size="small">正常</el-tag>
                    </template>
                </el-table-column>
                <el-table-column label="过期时间" width="160">
                    <template #default="{ row }">
                        {{ row.expiresAt ? formatDate(row.expiresAt) : '永不过期' }}
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="创建时间" width="160" />
                <el-table-column label="操作" width="200" fixed="right">
                    <template #default="{ row }">
                        <el-tooltip class="box-item" effect="dark" content="查看" placement="bottom">
                            <el-button size="small" :icon="View" circle @click="viewDetail(row)"></el-button>
                        </el-tooltip>
                        <el-tooltip class="box-item" effect="dark" content="重置" placement="bottom">
                            <el-button size="small" :icon="Refresh" circle type="warning" @click="resetSecret(row)"></el-button>
                        </el-tooltip>
                        <el-tooltip class="box-item" effect="dark" content="删除" placement="bottom">
                            <el-button size="small" :icon="Delete" circle type="danger" @click="deleteComment(row)"></el-button>
                        </el-tooltip>
                    </template>
                </el-table-column>
            </el-table>

            <div class="mt-5 flex justify-end">
                <el-pagination v-model:current-page="pagination.pageNum" v-model:page-size="pagination.pageSize"
                    :page-sizes="[10, 20, 50, 100]" :total="pagination.total" layout="total, sizes, prev, pager, next"
                    @size-change="handleSizeChange" @current-change="handlePageChange" />
            </div>
        </el-card>

        <!-- 查看详情弹窗 -->
        <el-dialog v-model="detailDialogVisible" title="私密评论详情" width="600px">
            <div v-if="currentComment" class="space-y-4">
                <div class="grid grid-cols-2 gap-4">
                    <div>
                        <label class="text-gray-500 text-sm">评论ID</label>
                        <div class="text-gray-900 dark:text-gray-100">{{ currentComment.id }}</div>
                    </div>
                    <div>
                        <label class="text-gray-500 text-sm">昵称</label>
                        <div class="text-gray-900 dark:text-gray-100">{{ currentComment.nickname }}</div>
                    </div>
                    <div>
                        <label class="text-gray-500 text-sm">状态</label>
                        <div class="text-gray-900 dark:text-gray-100">
                            <el-tag v-if="currentComment.isExpired" type="info" size="small">已过期</el-tag>
                            <el-tag v-else-if="currentComment.isReset" type="danger" size="small">已重置</el-tag>
                            <el-tag v-else type="success" size="small">正常</el-tag>
                        </div>
                    </div>
                    <div>
                        <label class="text-gray-500 text-sm">过期时间</label>
                        <div class="text-gray-900 dark:text-gray-100">{{ currentComment.expiresAt ? formatDate(currentComment.expiresAt) : '永不过期' }}</div>
                    </div>
                    <div>
                        <label class="text-gray-500 text-sm">创建时间</label>
                        <div class="text-gray-900 dark:text-gray-100">{{ formatDate(currentComment.createTime) }}</div>
                    </div>
                    <div>
                        <label class="text-gray-500 text-sm">IP地址</label>
                        <div class="text-gray-900 dark:text-gray-100">{{ currentComment.ipAddress || '-' }}</div>
                    </div>
                </div>
                <div>
                    <label class="text-gray-500 text-sm">评论内容</label>
                    <div class="mt-1 p-3 bg-gray-100 dark:bg-gray-700 rounded text-gray-900 dark:text-gray-100">
                        {{ currentComment.content }}
                    </div>
                </div>
                <div v-if="currentComment.imagesArray && currentComment.imagesArray.length > 0">
                    <label class="text-gray-500 text-sm">图片</label>
                    <div class="mt-1 flex flex-wrap gap-2">
                        <el-image 
                            v-for="(img, index) in currentComment.imagesArray" 
                            :key="index"
                            :src="img" 
                            :preview-src-list="currentComment.imagesArray"
                            :preview-teleported="true"
                            fit="cover"
                            class="w-20 h-20 rounded border cursor-pointer"
                            :initial-index="index"
                        />
                    </div>
                </div>
                <div v-if="currentComment.isSecret && !currentComment.isReset">
                    <label class="text-gray-500 text-sm">私密内容</label>
                    <div class="mt-1 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded text-gray-900 dark:text-gray-100">
                        <div class="text-xs text-amber-600 dark:text-amber-400 mb-2">以下为加密存储的私密内容，管理员可见</div>
                        <div class="whitespace-pre-wrap">{{ currentComment.secretContent || '(已过期或无法解密)' }}</div>
                    </div>
                </div>
            </div>
            <template #footer>
                <el-button @click="detailDialogVisible = false">关闭</el-button>
                <el-button type="warning" @click="resetSecretFromDetail">重置私密内容</el-button>
            </template>
        </el-dialog>

        <!-- 重置私密内容弹窗 -->
        <el-dialog v-model="resetDialogVisible" title="重置私密内容" width="500px">
            <div class="space-y-4">
                <div class="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-md">
                    <p class="text-sm text-amber-700 dark:text-amber-400">
                        <i class="fas fa-exclamation-triangle mr-1"></i>
                        重置后将清除原私密内容，无法恢复
                    </p>
                </div>
                <div>
                    <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">新私密内容</label>
                    <textarea v-model="resetForm.newContent" rows="3"
                        class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500 resize-none"
                        placeholder="留空则仅清除内容"></textarea>
                </div>
                <div>
                    <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">新密钥</label>
                    <input v-model="resetForm.newKey" type="text"
                        class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500"
                        placeholder="留空则仅清除内容">
                </div>
                <div>
                    <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">过期时间</label>
                    <select v-model="resetForm.expirationOption"
                        class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500">
                        <option value="never">永不过期</option>
                        <option value="1day">1天后过期</option>
                        <option value="7days">7天后过期</option>
                        <option value="30days">30天后过期</option>
                        <option value="custom">自定义时间</option>
                    </select>
                </div>
                <div v-if="resetForm.expirationOption === 'custom'">
                    <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">自定义过期时间</label>
                    <input v-model="resetForm.customExpiration" type="datetime-local"
                        class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500">
                </div>
            </div>
            <template #footer>
                <el-button @click="resetDialogVisible = false">取消</el-button>
                <el-button type="warning" @click="confirmReset" :loading="resetting">确认重置</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getSecretCommentList, resetSecretComment, deleteComment as deleteCommentApi } from '@/api/admin/comment'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, RefreshRight, View, Refresh, Delete } from '@element-plus/icons-vue'
import moment from 'moment'

const formatDate = (date) => {
    if (!date) return ''
    return moment(date).format('YYYY-MM-DD HH:mm')
}

const loading = ref(false)
const tableData = ref([])
const pagination = reactive({
    pageNum: 1,
    pageSize: 10,
    total: 0
})
const searchForm = reactive({
    nickname: '',
    dateRange: null
})

const detailDialogVisible = ref(false)
const resetDialogVisible = ref(false)
const resetting = ref(false)
const currentComment = ref(null)

const resetForm = reactive({
    newContent: '',
    newKey: '',
    expirationOption: 'never',
    customExpiration: ''
})

const getExpirationDate = () => {
    if (resetForm.expirationOption === 'never') return null
    const now = new Date()
    switch (resetForm.expirationOption) {
        case '1day': return new Date(now.getTime() + 24 * 60 * 60 * 1000)
        case '7days': return new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000)
        case '30days': return new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000)
        case 'custom': return resetForm.customExpiration ? new Date(resetForm.customExpiration) : null
        default: return null
    }
}

const fetchData = () => {
    loading.value = true
    const data = {
        pageNum: pagination.pageNum,
        pageSize: pagination.pageSize,
        nickname: searchForm.nickname || null,
        startDate: searchForm.dateRange ? searchForm.dateRange[0] : null,
        endDate: searchForm.dateRange ? searchForm.dateRange[1] : null
    }
    getSecretCommentList(data).then(res => {
        if (res.success) {
            tableData.value = (res.data.list || []).map(item => {
                if (item.images) {
                    item.imagesArray = item.images.split(',').filter(img => img.trim())
                } else {
                    item.imagesArray = []
                }
                return item
            })
            pagination.total = res.data.total || 0
        }
    }).finally(() => {
        loading.value = false
    })
}

const search = () => {
    pagination.pageNum = 1
    fetchData()
}

const reset = () => {
    searchForm.nickname = ''
    searchForm.dateRange = null
    pagination.pageNum = 1
    fetchData()
}

const handleSizeChange = () => {
    pagination.pageNum = 1
    fetchData()
}

const handlePageChange = () => {
    fetchData()
}

const viewDetail = (row) => {
    if (row.images) {
        row.imagesArray = row.images.split(',').filter(img => img.trim())
    } else {
        row.imagesArray = []
    }
    currentComment.value = row
    detailDialogVisible.value = true
}

const resetSecret = (row) => {
    currentComment.value = row
    resetForm.newContent = ''
    resetForm.newKey = ''
    resetForm.expirationOption = 'never'
    resetForm.customExpiration = ''
    resetDialogVisible.value = true
}

const resetSecretFromDetail = () => {
    detailDialogVisible.value = false
    resetDialogVisible.value = true
}

const confirmReset = () => {
    if (!resetForm.newContent && !resetForm.newKey) {
        ElMessage.warning('请输入新内容或新密钥')
        return
    }
    resetting.value = true
    resetSecretComment({
        commentId: currentComment.value.id,
        newSecretContent: resetForm.newContent || null,
        newSecretKey: resetForm.newKey || null,
        expiresAt: getExpirationDate()
    }).then(res => {
        if (res.success) {
            ElMessage.success('重置成功')
            resetDialogVisible.value = false
            fetchData()
        } else {
            ElMessage.error(res.message || '重置失败')
        }
    }).finally(() => {
        resetting.value = false
    })
}

const deleteComment = (row) => {
    ElMessageBox.confirm('确定要删除这条评论吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
    }).then(() => {
        deleteCommentApi(row.id).then(res => {
            if (res.success) {
                ElMessage.success('删除成功')
                fetchData()
            } else {
                ElMessage.error(res.message || '删除失败')
            }
        })
    }).catch(() => {})
}

onMounted(() => {
    fetchData()
})
</script>
