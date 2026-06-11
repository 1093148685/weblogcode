<template>
    <div>
        <el-card shadow="never" class="mb-5">
            <div class="flex items-center flex-wrap gap-3">
                <el-text>文章标题</el-text>
                <div class="w-52"><el-input v-model="searchArticleTitle" placeholder="请输入（模糊查询）" size="default" /></div>

                <el-text>创建日期</el-text>
                <div class="w-72">
                    <el-date-picker v-model="pickDate" type="daterange" range-separator="至" start-placeholder="开始时间"
                        end-placeholder="结束时间" size="default" :shortcuts="shortcuts" @change="datepickerChange" style="width: 100%" />
                </div>

                <el-button type="primary" class="ml-auto" :icon="Search" @click="getTableData">查询</el-button>
                <el-button :icon="RefreshRight" @click="reset">重置</el-button>
            </div>
        </el-card>

        <el-card shadow="never">
            <div class="mb-5">
                <el-button type="primary" @click="openCreateEditor">
                    <el-icon class="mr-1"><EditPen /></el-icon>
                    写文章
                </el-button>
            </div>

            <el-table :data="tableData" border stripe style="width: 100%" v-loading="tableLoading">
                <el-table-column prop="id" label="ID" width="72" align="center" />
                <el-table-column prop="title" label="标题" min-width="360" show-overflow-tooltip />
                <el-table-column prop="cover" label="封面" width="140" align="center">
                    <template #default="scope">
                        <el-image style="width: 100px;" :src="scope.row.cover" />
                    </template>
                </el-table-column>
                <el-table-column prop="isTop" label="置顶" width="100" align="center">
                    <template #default="scope">
                        <el-switch @change="handleIsTopChange(scope.row)" v-model="scope.row.isTop"
                            inline-prompt :active-icon="Check" :inactive-icon="Close" />
                    </template>
                </el-table-column>
                <el-table-column prop="status" label="发布状态" width="120" align="center">
                    <template #default="scope">
                        <el-switch :model-value="scope.row.status === 1" inline-prompt
                            active-text="发布" inactive-text="草稿"
                            @change="value => handleArticleStatusChange(scope.row, value)" />
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="发布时间" width="190" show-overflow-tooltip />
                <el-table-column label="操作" width="190" fixed="right">
                    <template #default="scope">
                        <div class="table-action-group">
                            <el-button size="small" @click="openEditEditor(scope.row)">
                                <el-icon class="mr-1"><Edit /></el-icon>编辑
                            </el-button>
                            <el-button size="small" @click="goArticleDetailPage(scope.row.id)">
                                <el-icon class="mr-1"><View /></el-icon>预览
                            </el-button>
                            <el-button type="danger" size="small" @click="deleteArticleSubmit(scope.row)">
                                <el-icon class="mr-1"><Delete /></el-icon>
                            </el-button>
                        </div>
                    </template>
                </el-table-column>
            </el-table>

            <div class="mt-10 flex justify-center">
                <el-pagination v-model:current-page="current" v-model:page-size="size" :page-sizes="[10, 20, 50]"
                    :small="false" :background="true" layout="total, sizes, prev, pager, next, jumper" :total="total"
                    @size-change="handleSizeChange" @current-change="getTableData" />
            </div>
        </el-card>

        <!-- 文章编辑器 -->
        <ArticleEditor v-model="editorVisible" :article-id="editingArticleId" @saved="getTableData" />
    </div>
</template>

<script setup>
defineOptions({ name: 'AdminArticleList' })
import { ref, reactive } from 'vue'
import { Search, RefreshRight, Check, Close, EditPen, Edit, View, Delete, Promotion } from '@element-plus/icons-vue'
import { getArticlePageList, deleteArticle, updateArticleIsTop, updateArticleStatus } from '@/api/admin/article'
import moment from 'moment'
import { showMessage, showModel } from '@/composables/util'
import { useRouter } from 'vue-router'
import ArticleEditor from '@/components/ArticleEditor.vue'

const router = useRouter()

const searchArticleTitle = ref('')
const pickDate = ref('')
const startDate = reactive({})
const endDate = reactive({})

const datepickerChange = (e) => {
    startDate.value = moment(e[0]).format('YYYY-MM-DD')
    endDate.value = moment(e[1]).format('YYYY-MM-DD')
}

const shortcuts = [
    { text: '最近一周', value: () => { const end = new Date(); const start = new Date(); start.setTime(start.getTime() - 3600 * 1000 * 24 * 7); return [start, end] } },
    { text: '最近一个月', value: () => { const end = new Date(); const start = new Date(); start.setTime(start.getTime() - 3600 * 1000 * 24 * 30); return [start, end] } },
    { text: '最近三个月', value: () => { const end = new Date(); const start = new Date(); start.setTime(start.getTime() - 3600 * 1000 * 24 * 90); return [start, end] } },
]

const reset = () => {
    pickDate.value = ''; startDate.value = null; endDate.value = null; searchArticleTitle.value = ''
}

const tableLoading = ref(false)
const tableData = ref([])
const current = ref(1)
const total = ref(0)
const size = ref(10)

function getTableData() {
    tableLoading.value = true
    getArticlePageList({ pageNum: current.value, pageSize: size.value, startDate: startDate.value, endDate: endDate.value, title: searchArticleTitle.value }, true)
        .then((res) => {
            if (res.success == true) {
                tableData.value = res.data.list
                current.value = res.data.pageNum
                size.value = res.data.pageSize
                total.value = res.data.total
            }
        })
        .finally(() => tableLoading.value = false)
}
getTableData()

const handleSizeChange = (chooseSize) => { size.value = chooseSize; getTableData() }

const deleteArticleSubmit = (row) => {
    showModel('是否确定要删除该文章？').then(() => {
        deleteArticle(row.id).then((res) => {
            if (res.success == false) { showMessage(res.message, 'error'); return }
            showMessage('删除成功'); getTableData()
        })
    }).catch(() => {})
}

// 编辑器
const editorVisible = ref(false)
const editingArticleId = ref(null)

const openCreateEditor = () => {
    editingArticleId.value = null
    editorVisible.value = true
}

const openEditEditor = (row) => {
    editingArticleId.value = row.id
    editorVisible.value = true
}

const goArticleDetailPage = (articleId) => { router.push('/article/' + articleId) }

const handleIsTopChange = (row) => {
    updateArticleIsTop({id: row.id, isTop: row.isTop}).then((res) => {
        getTableData()
        if (res.success == false) { showMessage(res.message, 'error'); return }
        showMessage(row.isTop ? '置顶成功' : "已取消置顶")
    })
}

const handleArticleStatusChange = (row, isPublished) => {
    const previousStatus = row.status
    row.status = isPublished ? 1 : 0
    updateArticleStatus({ id: row.id, status: row.status }).then((res) => {
        if (res.success == false) { row.status = previousStatus; showMessage(res.message, 'error'); return }
        showMessage(isPublished ? '已发布到前台' : '已转为草稿'); getTableData()
    }).catch(() => { row.status = previousStatus; showMessage('更新发布状态失败', 'error') })
}
</script>

<style scoped>
.table-action-group {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    white-space: nowrap;
}
.table-action-group .el-button + .el-button { margin-left: 0; }
</style>
