<template>
    <div>
        <!-- 搜索条件 -->
        <el-card shadow="never" class="mb-5">
            <div class="flex items-center">
                <el-text>日志标题</el-text>
                <div class="ml-3 w-52 mr-5"><el-input v-model="searchTitle" placeholder="请输入（模糊查询）" /></div>

                <el-text>创建日期</el-text>
                <div class="ml-3 w-30 mr-5">
                    <el-date-picker v-model="pickDate" type="daterange" range-separator="至" start-placeholder="开始时间"
                        end-placeholder="结束时间" size="default" :shortcuts="shortcuts" @change="datepickerChange" />
                </div>

                <el-button type="primary" class="ml-3" :icon="Search" @click="getTableData">查询</el-button>
                <el-button class="ml-3" :icon="RefreshRight" @click="reset">重置</el-button>
            </div>
        </el-card>

        <el-card shadow="never">
            <!-- 写日志按钮 -->
            <div class="mb-5">
                <el-button type="primary" @click="isCreateEditorShow = true">
                    <el-icon class="mr-1">
                        <EditPen />
                    </el-icon>
                    写日志</el-button>
            </div>

            <!-- 分页列表 -->
            <el-table :data="filteredTableData" border stripe style="width: 100%" v-loading="tableLoading">
                <el-table-column prop="id" label="ID" width="70" />
                <el-table-column prop="title" label="标题" min-width="300" />
                <el-table-column prop="createTime" label="日期" width="180" />
                <el-table-column prop="readNum" label="阅读量" width="100" />
                <el-table-column label="操作" width="280">
                    <template #default="scope">
                        <el-button size="small" @click="showEditEditor(scope.row)">
                            <el-icon class="mr-1">
                                <Edit />
                            </el-icon>
                            编辑</el-button>
                        <el-button size="small" @click="goDetailPage(scope.row.id)">
                            <el-icon class="mr-1">
                                <View />
                            </el-icon>
                            预览</el-button>
                        <el-button type="danger" size="small" @click="deleteJournalSubmit(scope.row)">
                            <el-icon class="mr-1">
                                <Delete />
                            </el-icon>
                            删除
                        </el-button>
                    </template>
                </el-table-column>
            </el-table>

            <!-- 分页 -->
            <div class="mt-10 flex justify-center">
                <el-pagination v-model:current-page="current" v-model:page-size="size" :page-sizes="[10, 20, 50]"
                    :small="false" :background="true" layout="total, sizes, prev, pager, next, jumper" :total="total"
                    @size-change="handleSizeChange" @current-change="getTableData" />
            </div>
        </el-card>

        <!-- 写日志弹窗 -->
        <el-dialog v-model="isCreateEditorShow" :fullscreen="true" :show-close="false"
            :close-on-press-escape="false">
            <template #header="{ close, titleId, titleClass }">
                <el-affix :offset="20" style="width: 100%;">
                    <div class="flex h-10 bg-white">
                        <h4 class="font-bold">写日志</h4>
                        <div class="ml-auto flex">
                            <el-button @click="isCreateEditorShow = false">取消</el-button>
                            <el-button type="primary" @click="createSubmit">
                                <el-icon class="mr-1">
                                    <Promotion />
                                </el-icon>
                                发布
                            </el-button>
                        </div>
                    </div>
                </el-affix>
            </template>
            <el-form :model="createForm" ref="createFormRef" label-position="top" size="large" :rules="rules">
                <el-form-item label="标题" prop="title">
                    <el-input v-model="createForm.title" autocomplete="off" size="large" maxlength="60" show-word-limit
                        clearable />
                </el-form-item>
                <el-form-item label="内容" prop="content">
                    <MdEditor v-model="createForm.content" @onUploadImg="onUploadImg" editorId="createJournalEditor" />
                </el-form-item>
            </el-form>
        </el-dialog>

        <!-- 编辑日志弹窗 -->
        <el-dialog v-model="isEditEditorShow" :fullscreen="true" :show-close="false"
            :close-on-press-escape="false">
            <template #header="{ close, titleId, titleClass }">
                <el-affix :offset="20" style="width: 100%;">
                    <div class="flex h-10 bg-white">
                        <h4 class="font-bold">编辑日志</h4>
                        <div class="ml-auto flex">
                            <el-button @click="isEditEditorShow = false">取消</el-button>
                            <el-button type="primary" @click="updateSubmit">
                                <el-icon class="mr-1">
                                    <Promotion />
                                </el-icon>
                                保存
                            </el-button>
                        </div>
                    </div>
                </el-affix>
            </template>
            <el-form :model="editForm" ref="editFormRef" label-position="top" size="large" :rules="rules">
                <el-form-item label="标题" prop="title">
                    <el-input v-model="editForm.title" autocomplete="off" size="large" maxlength="60" show-word-limit
                        clearable />
                </el-form-item>
                <el-form-item label="内容" prop="content">
                    <MdEditor v-model="editForm.content" @onUploadImg="onUploadImg" editorId="editJournalEditor" />
                </el-form-item>
            </el-form>
        </el-dialog>
    </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { Search, RefreshRight } from '@element-plus/icons-vue'
import { getJournalList, createJournal, updateJournal, deleteJournal, getJournalDetail } from '@/api/admin/journal'
import { uploadFile } from '@/api/admin/file'
import moment from 'moment'
import { showMessage, showModel } from '@/composables/util'
import { setCache, getCache, clearCacheByPrefix } from '@/composables/useCache'
import { MdEditor } from 'md-editor-v3'
import 'md-editor-v3/lib/style.css'
import { useRouter } from 'vue-router'

const router = useRouter()

// 搜索条件
const searchTitle = ref('')
const pickDate = ref('')
const startDate = reactive({})
const endDate = reactive({})

const datepickerChange = (e) => {
    startDate.value = moment(e[0]).format('YYYY-MM-DD')
    endDate.value = moment(e[1]).format('YYYY-MM-DD')
}

const shortcuts = [
    {
        text: '最近一周',
        value: () => {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 7)
            return [start, end]
        },
    },
    {
        text: '最近一个月',
        value: () => {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 30)
            return [start, end]
        },
    },
    {
        text: '最近三个月',
        value: () => {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 90)
            return [start, end]
        },
    },
]

const reset = () => {
    pickDate.value = ''
    startDate.value = null
    endDate.value = null
    searchTitle.value = ''
}

// 表格数据
const tableLoading = ref(false)
const tableData = ref([])
const current = ref(1)
const total = ref(0)
const size = ref(10)

// 客户端过滤（标题模糊搜索 + 日期范围）
const filteredTableData = computed(() => {
    let list = tableData.value
    if (searchTitle.value) {
        list = list.filter(item => item.title && item.title.includes(searchTitle.value))
    }
    if (startDate.value) {
        list = list.filter(item => item.createTime && item.createTime >= startDate.value)
    }
    if (endDate.value) {
        list = list.filter(item => item.createTime && item.createTime <= endDate.value + ' 23:59:59')
    }
    return list
})

function getTableData() {
    const cacheKey = `admin_journals_${current.value}_${size.value}`

    const cached = getCache(cacheKey)
    if (cached) {
        tableData.value = cached.list
        current.value = cached.pageNum
        size.value = cached.pageSize
        total.value = cached.total
        tableLoading.value = false
    } else {
        tableLoading.value = true
    }

    getJournalList({ pageNum: current.value, pageSize: size.value })
        .then((res) => {
            if (res.success == true) {
                tableData.value = res.data.list
                current.value = res.data.pageNum
                size.value = res.data.pageSize
                total.value = res.data.total
                setCache(cacheKey, {
                    list: res.data.list,
                    pageNum: res.data.pageNum,
                    pageSize: res.data.pageSize,
                    total: res.data.total
                }, 30 * 1000)
            }
        })
        .finally(() => tableLoading.value = false)
}
getTableData()

const handleSizeChange = (chooseSize) => {
    size.value = chooseSize
    getTableData()
}

// 删除
const deleteJournalSubmit = (row) => {
    showModel('是否确定要删除该日志？').then(() => {
        deleteJournal(row.id).then((res) => {
            if (res.success == false) {
                showMessage(res.message, 'error')
                return
            }
            showMessage('删除成功')
            clearCacheByPrefix('admin_journals_')
            clearCacheByPrefix('journals_page_')
            getTableData()
        })
    }).catch(() => {
        console.log('取消了')
    })
}

// 创建日志
const isCreateEditorShow = ref(false)
const createFormRef = ref(null)
const createForm = reactive({
    title: '',
    content: '请输入内容',
})

const rules = {
    title: [
        { required: true, message: '请输入日志标题', trigger: 'blur' },
        { min: 1, max: 60, message: '日志标题要求大于1个字符，小于60个字符', trigger: 'blur' },
    ],
    content: [{ required: true }],
}

const createSubmit = () => {
    createFormRef.value.validate((valid) => {
        if (!valid) return false

        createJournal({ title: createForm.title, content: createForm.content }).then((res) => {
            if (res.success == false) {
                showMessage(res.message, 'error')
                return
            }

            showMessage('发布成功')
            clearCacheByPrefix('admin_journals_')
            clearCacheByPrefix('journals_page_')
            isCreateEditorShow.value = false
            createForm.title = ''
            createForm.content = ''
            getTableData()
        })
    })
}

// 编辑日志
const isEditEditorShow = ref(false)
const editFormRef = ref(null)
const editForm = reactive({
    id: null,
    title: '',
    content: '',
})

const showEditEditor = (row) => {
    isEditEditorShow.value = true
    getJournalDetail(row.id).then((res) => {
        if (res.success) {
            editForm.id = res.data.id
            editForm.title = res.data.title
            editForm.content = res.data.content
        }
    })
}

const updateSubmit = () => {
    editFormRef.value.validate((valid) => {
        if (!valid) return false

        updateJournal({ id: editForm.id, title: editForm.title, content: editForm.content }).then((res) => {
            if (res.success == false) {
                showMessage(res.message, 'error')
                return
            }

            showMessage('保存成功')
            clearCacheByPrefix('admin_journals_')
            clearCacheByPrefix('journals_page_')
            isEditEditorShow.value = false
            getTableData()
        })
    })
}

// 图片上传
const onUploadImg = async (files, callback) => {
    const res = await Promise.all(
        files.map((file) => {
            return new Promise((rev, rej) => {
                let formData = new FormData()
                formData.append("file", file)
                uploadFile(formData).then((res) => {
                    callback([res.data])
                    rev(res.data)
                }).catch(rej)
            })
        })
    )
}

// 跳转详情页
const goDetailPage = (id) => {
    router.push('/journal/' + id)
}
</script>
