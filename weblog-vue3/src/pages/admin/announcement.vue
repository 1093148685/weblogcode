<template>
    <div>
        <el-card shadow="never">
            <!-- 搜索条件 -->
            <div class="flex items-center mb-4">
                <el-text>公告内容</el-text>
                <div class="ml-3 w-52 mr-5">
                    <el-input v-model="searchContent" placeholder="请输入（模糊查询）" clearable />
                </div>

                <el-text>创建日期</el-text>
                <div class="ml-3 w-60 mr-5">
                    <el-date-picker v-model="pickDate" type="daterange" range-separator="至" start-placeholder="开始时间"
                        end-placeholder="结束时间" value-format="YYYY-MM-DD" @change="datepickerChange" />
                </div>

                <el-text>是否展示</el-text>
                <div class="ml-3 w-40 mr-5">
                    <el-select v-model="searchIsEnabled" placeholder="---请选择---" clearable>
                        <el-option label="是" :value="true" />
                        <el-option label="否" :value="false" />
                    </el-select>
                </div>

                <el-button type="primary" :icon="Search" @click="getTableData">搜索</el-button>
                <el-button :icon="RefreshRight" @click="resetSearch">重置</el-button>
                <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新建公告</el-button>
            </div>

            <!-- 表格 -->
            <el-table :data="tableData" border stripe>
                <el-table-column prop="id" label="编号" width="80" />
                <el-table-column prop="content" label="公告内容" min-width="300" show-overflow-tooltip />
                <el-table-column prop="isEnabled" label="是否展示" width="100">
                    <template #default="{ row }">
                        <el-tag v-if="row.isEnabled" type="success">是</el-tag>
                        <el-tag v-else type="info">否</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="创建时间" width="180" />
                <el-table-column label="操作" width="180" fixed="right">
                    <template #default="{ row }">
                        <el-button type="primary" link @click="handleEdit(row)">编辑</el-button>
                        <el-button type="danger" link :disabled="!isAdmin()" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>

            <!-- 分页 -->
            <div class="mt-10 flex justify-center">
                <el-pagination v-model:current-page="current" v-model:page-size="size" :page-sizes="[10, 20, 50]"
                    :small="false" :background="true" layout="total, sizes, prev, pager, next, jumper" :total="total"
                    @size-change="getTableData" @current-change="getTableData" />
            </div>

            <!-- 新增/编辑对话框 -->
            <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
                <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
                    <el-form-item label="公告内容" prop="content">
                        <el-input v-model="form.content" type="textarea" :rows="4" placeholder="请输入公告内容" />
                    </el-form-item>
                    <el-form-item label="是否展示">
                        <el-switch v-model="form.isEnabled" inline-prompt :active-icon="Check" :inactive-icon="Close" />
                    </el-form-item>
                </el-form>
                <template #footer>
                    <el-button @click="dialogVisible = false">取消</el-button>
                    <el-button type="primary" @click="handleSubmit">确定</el-button>
                </template>
            </el-dialog>
        </el-card>
    </div>
</template>

<script setup>
defineOptions({
    name: 'AdminAnnouncement'
})
import { ref, onMounted } from 'vue'
import { Search, RefreshRight, Check, Close, Plus } from '@element-plus/icons-vue'
import { getAnnouncementList, createAnnouncement, updateAnnouncement, deleteAnnouncement } from '@/api/admin/announcement'
import { showMessage, showModel, isAdmin } from '@/composables/util'

const tableData = ref([])
const current = ref(1)
const size = ref(10)
const total = ref(0)

// 搜索条件
const searchContent = ref('')
const searchIsEnabled = ref('')
const pickDate = ref('')
const startDate = ref('')
const endDate = ref('')

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增公告')
const formRef = ref(null)

const form = ref({
    id: null,
    content: '',
    isEnabled: true
})

const rules = {
    content: [{ required: true, message: '请输入公告内容', trigger: 'blur' }]
}

onMounted(() => {
    getTableData()
})

// 数据是否已加载
const dataLoaded = ref(false)

function getTableData() {
    // 如果数据已加载且有数据，不重复加载
    if (dataLoaded.value && tableData.value.length > 0) {
        return
    }
    getAnnouncementList({
        content: searchContent.value || null,
        isEnabled: searchIsEnabled.value || null,
        startDate: startDate.value || null,
        endDate: endDate.value || null,
        pageNum: current.value,
        pageSize: size.value
    }).then(res => {
        if (res.success) {
            tableData.value = res.data.list || []
            total.value = res.data.total || 0
            dataLoaded.value = true
        }
    })
}

function datepickerChange(value) {
    if (value && value.length === 2) {
        startDate.value = value[0]
        endDate.value = value[1]
    } else {
        startDate.value = ''
        endDate.value = ''
    }
}

function resetSearch() {
    searchContent.value = ''
    searchIsEnabled.value = ''
    pickDate.value = ''
    startDate.value = ''
    endDate.value = ''
    current.value = 1
    getTableData()
}

function handleAdd() {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    dialogTitle.value = '新增公告'
    form.value = {
        id: null,
        content: '',
        isEnabled: true
    }
    dialogVisible.value = true
}

function handleEdit(row) {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    dialogTitle.value = '编辑公告'
    form.value = {
        id: row.id,
        content: row.content,
        isEnabled: row.isEnabled
    }
    dialogVisible.value = true
}

function handleDelete(row) {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    showModel('确定要删除该公告吗？').then(() => {
        deleteAnnouncement(row.id).then(res => {
            if (res.success) {
                showMessage('删除成功', 'success')
                dataLoaded.value = false
                getTableData()
            }
        })
    })
}

function handleSubmit() {
    formRef.value.validate(valid => {
        if (valid) {
            if (form.value.id) {
                updateAnnouncement(form.value).then(res => {
                    if (res.success) {
                        showMessage('更新成功', 'success')
                        dialogVisible.value = false
                        dataLoaded.value = false
                        getTableData()
                    }
                })
            } else {
                createAnnouncement(form.value).then(res => {
                    if (res.success) {
                        showMessage('创建成功', 'success')
                        dialogVisible.value = false
                        dataLoaded.value = false
                        getTableData()
                    }
                })
            }
        }
    })
}
</script>
