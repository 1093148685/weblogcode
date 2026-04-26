<template>
    <div>
        <!-- 表头分页查询条件， shadow="never" 指定 card 卡片组件没有阴影 -->
        <el-card shadow="never" class="mb-5">
            <!-- flex 布局，内容垂直居中 -->
            <div class="flex items-center">
                <el-text>路由地址</el-text>
                <div class="ml-3 w-52 mr-5"><el-input v-model="searchRouterUrl" placeholder="请输入（模糊查询）" clearable />
                </div>

                <el-text>创建日期</el-text>
                <div class="ml-3 w-30 mr-5">
                    <!-- 日期选择组件（区间选择） -->
                    <el-date-picker v-model="pickDate" type="daterange" range-separator="至" start-placeholder="开始时间"
                        end-placeholder="结束时间" size="default" :shortcuts="shortcuts" @change="datepickerChange" />
                </div>

                <el-text>状态</el-text>
                <div class="ml-3 w-30 mr-5">
                    <el-select v-model="status" placeholder="---请选择---">
                        <el-option v-for="item in statusOptions" :key="item.value" :label="item.label"
                            :value="item.value" />
                    </el-select>
                </div>

                <el-button type="primary" class="ml-3" :icon="Search" @click="handleSearch">查询</el-button>
                <el-button class="ml-3" :icon="RefreshRight" @click="reset">重置</el-button>
            </div>
        </el-card>

        <el-card shadow="never">
            <!-- 批量删除按钮 -->
            <div class="mb-5" v-if="selectedComments.length > 0">
                <el-button type="danger" @click="batchDeleteSubmit">
                    批量删除 ({{ selectedComments.length }})
                </el-button>
            </div>

            <!-- 分页列表 -->
            <el-table :data="tableData" border stripe v-loading="tableLoading" table-layout="auto" @selection-change="handleSelectionChange">
                <el-table-column type="selection" width="55" />
                <el-table-column prop="routerUrl" label="路由">
                    <template #default="scope">
                        <el-link type="primary" :href="'#' + scope.row.routerUrl" target="_blank">{{ scope.row.routerUrl
                            }}</el-link>
                    </template>
                </el-table-column>
                <el-table-column prop="avatar" label="头像" width="60">
                    <template #default="scope">
                        <el-avatar :size="40" :src="scope.row.avatar" />
                    </template>
                </el-table-column>
                <el-table-column prop="nickname" label="昵称">
                    <template #default="scope">
                        <div class="flex items-center gap-2">
                            <span>{{ scope.row.nickname }}</span>
                            <el-tag v-if="scope.row.isAdmin" size="small" type="warning">
                                <i class="fas fa-crown mr-1"></i>管理员
                            </el-tag>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="content" label="评论内容" show-overflow-tooltip />
                <el-table-column label="图片" width="100">
                    <template #default="scope">
                        <div v-if="scope.row.images && scope.row.images.length > 0" class="flex items-center gap-1">
                            <el-image 
                                v-if="scope.row.imagesArray && scope.row.imagesArray[0]" 
                                :src="scope.row.imagesArray[0]" 
                                :preview-src-list="scope.row.imagesArray"
                                :preview-teleported="true"
                                fit="cover"
                                class="w-10 h-10 rounded border cursor-pointer"
                                :initial-index="0"
                            />
                            <span v-if="scope.row.imagesArray && scope.row.imagesArray.length > 1" class="text-xs text-gray-500">
                                +{{ scope.row.imagesArray.length - 1 }}
                            </span>
                        </div>
                        <span v-else class="text-gray-400 text-xs">无</span>
                    </template>
                </el-table-column>
                <el-table-column prop="flowerCount" label="送花" width="80">
                    <template #default="scope">
                        <span class="text-pink-500">🌸 {{ scope.row.flowerCount || 0 }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="发布时间" width="200" />
                <el-table-column prop="status" label="状态">
                    <template #default="scope">
                        <el-tag type="warning" v-if="scope.row.status == 1">待审核</el-tag>
                        <el-tag type="success" v-else-if="scope.row.status == 2">正常</el-tag>
                        <el-tag type="danger" v-else-if="scope.row.status == 3">审核不通过</el-tag>
                    </template>
                </el-table-column>
                <el-table-column fixed="right" label="操作" width="150">
                    <template #default="scope">

                        <el-tooltip class="box-item" effect="dark" content="详情" placement="bottom">
                            <el-button size="small" :icon="Tickets" circle @click="showDetailDialog(scope.row)">
                            </el-button>
                        </el-tooltip>

                        <el-tooltip v-if="scope.row.status == 1" class="box-item" effect="dark" content="审核"
                            placement="bottom">
                            <el-button size="small" :icon="Edit" circle @click="showEditDetailDialog(scope.row)">
                            </el-button>
                        </el-tooltip>

                        <el-tooltip class="box-item" effect="dark" content="删除" placement="bottom">
                            <el-button type="danger" size="small" :icon="Delete" @click="deleteCommentSubmit(scope.row)"
                                circle>
                            </el-button>
                        </el-tooltip>

                    </template>
                </el-table-column>
            </el-table>

            <!-- 分页 -->
            <div class="mt-10 flex justify-center">
                <el-pagination v-model:current-page="current" v-model:page-size="size" :page-sizes="[10, 20, 50]"
                    :small="false" :background="true" layout="total, sizes, prev, pager, next, jumper" :total="total"
                    @size-change="handleSizeChange" @current-change="handleCurrentChange" />
            </div>

        </el-card>

        <!-- 查看评论详情 -->
        <el-dialog v-model="detailDialogVisible" title="评论详情" width="700">
            <el-form :model="commentDetail" label-width="auto">
                <el-form-item label="路由">
                    <el-input v-model="commentDetail.routerUrl" disabled />
                </el-form-item>
                <el-form-item label="头像">
                    <el-avatar :size="60" :src="commentDetail.avatar" />
                </el-form-item>
                <el-form-item label="昵称">
                    <el-input v-model="commentDetail.nickname" disabled />
                </el-form-item>

                <el-form-item label="评论内容">
                    <el-input type="textarea" v-model="commentDetail.content" disabled :rows="4" />
                </el-form-item>
                <el-form-item label="图片" v-if="commentDetail.imagesArray && commentDetail.imagesArray.length > 0">
                    <div class="flex flex-wrap gap-2">
                        <el-image 
                            v-for="(img, index) in commentDetail.imagesArray" 
                            :key="index"
                            :src="img" 
                            :preview-src-list="commentDetail.imagesArray"
                            :preview-teleported="true"
                            fit="cover"
                            class="w-20 h-20 rounded border cursor-pointer"
                            :initial-index="index"
                        />
                    </div>
                </el-form-item>
                <el-form-item label="网站">
                    <el-input v-model="commentDetail.website" disabled />
                </el-form-item>
                <el-form-item label="邮箱">
                    <el-input v-model="commentDetail.mail" disabled />
                </el-form-item>
                
                <el-form-item label="设备信息">
                    <div class="flex gap-2">
                        <el-tag v-if="commentDetail.deviceType" type="info">{{ commentDetail.deviceType }}</el-tag>
                        <el-tag v-if="commentDetail.browser" type="info">{{ commentDetail.browser }}</el-tag>
                    </div>
                </el-form-item>
                <el-form-item label="IP 属地" v-if="commentDetail.ipLocation || commentDetail.ipAddress">
                    <span>{{ commentDetail.ipLocation || '未知' }}</span>
                    <span class="text-gray-400 text-xs ml-2" v-if="commentDetail.ipAddress">({{ commentDetail.ipAddress }})</span>
                </el-form-item>
                <el-form-item label="送花数">
                    <span class="text-pink-500">🌸 {{ commentDetail.flowerCount || 0 }}</span>
                </el-form-item>
                <el-form-item label="发布时间">
                    <el-input v-model="commentDetail.createTime" disabled />
                </el-form-item>
                <el-form-item label="状态">
                    <el-tag type="warning" v-if="commentDetail.status == 1">待审核</el-tag>
                    <el-tag type="success" v-else-if="commentDetail.status == 2">正常</el-tag>
                    <el-tag type="danger" v-else-if="commentDetail.status == 3">审核不通过</el-tag>
                </el-form-item>
                <el-form-item label="审核原因" v-if="commentDetail.reason">
                    <el-input type="textarea" v-model="commentDetail.reason" disabled :rows="2" />
                </el-form-item>
            </el-form>
            <template #footer>
                <div class="dialog-footer">
                    <el-button @click="detailDialogVisible = false">退出</el-button>
                </div>
            </template>
        </el-dialog>

        <!-- 评论审核 -->
        <FormDialog ref="editDialogRef" title="审核评论" destroyOnClose @submit="onSubmit">
            <el-form ref="formRef" :rules="rules" :model="form" label-width="auto">
                <el-form-item label="状态" prop="status">
                    <el-radio-group v-model="form.status">
                        <el-radio label="2">通过</el-radio>
                        <el-radio label="3">不通过</el-radio>
                    </el-radio-group>
                </el-form-item>
                <el-form-item label="原因" prop="reason" v-if="form.status == 3">
                    <el-input type="textarea" placeholder="请填写审核不通过的原因" v-model="form.reason" :rows="6" />
                </el-form-item>
            </el-form>
        </FormDialog>
    </div>
</template>

<script setup>
defineOptions({
    name: 'AdminCommentList'
})
import { ref, reactive, onMounted } from 'vue'
import { getCommentPageList, deleteComment, examineComment, batchDeleteComment } from '@/api/admin/comment'
import { Search, RefreshRight, Delete, Edit, Tickets } from '@element-plus/icons-vue'
import moment from 'moment'
import { showMessage, showModel } from '@/composables/util'
import FormDialog from '@/components/FormDialog.vue'

// 模糊搜索的路由
const searchRouterUrl = ref('')
// 日期
const pickDate = ref('')

// 查询条件：开始结束时间
const startDate = reactive({})
const endDate = reactive({})

// 监听日期组件改变事件，并将开始结束时间设置到变量中
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

// 当前选择的评论状态
const status = ref(null)
// 评论状态 select
const statusOptions = [
    {
        value: 1,
        label: '待审核',
    },
    {
        value: 2,
        label: '正常',
    },
    {
        value: 3,
        label: '审核未通过',
    },
]

// 重置
const reset = () => {
    pickDate.value = ''
    startDate.value = null
    endDate.value = null
    searchRouterUrl.value = ''
    status.value = null
    current.value = 1
    dataLoaded.value = false
    getTableData()
}

// 表格加载 Loading
const tableLoading = ref(false)
// 表格数据
const tableData = ref([])
// 选中的评论
const selectedComments = ref([])

// 表格选择事件
const handleSelectionChange = (comments) => {
    selectedComments.value = comments
}

// 批量删除评论
const batchDeleteSubmit = () => {
    const ids = selectedComments.value.map(item => item.id)
    showModel('是否确定要删除选中的 ' + ids.length + ' 条评论？').then(() => {
        batchDeleteComment(ids).then((res) => {
            if (!res.success) {
                showMessage(res.message, 'error')
                return
            }
            showMessage('批量删除成功')
            // 重新请求分页接口，渲染数据
            dataLoaded.value = false
            getTableData()
        })
    })
}

// 当前页码，给了一个默认值 1
const current = ref(1)
// 总数据量，给了个默认值 0
const total = ref(0)
// 每页显示的数据量，给了个默认值 10
const size = ref(10)

// 数据是否已加载
const dataLoaded = ref(false)

// 获取分页数据
function getTableData() {
    // 显示表格 loading
    tableLoading.value = true
    // 调用后台分页接口，并传入所需参数
    getCommentPageList({
        pageNum: current.value, pageSize: size.value, startDate: startDate.value,
        endDate: endDate.value, nickname: searchRouterUrl.value, status: status.value
    })
        .then((res) => {
            if (res.success == true) {
                tableData.value = res.data.list.map(item => {
                    if (item.images) {
                        item.imagesArray = item.images.split(',').filter(img => img.trim())
                    } else {
                        item.imagesArray = []
                    }
                    return item
                })
                current.value = res.data.pageNum
                size.value = res.data.pageSize
                total.value = res.data.total
                dataLoaded.value = true
            }
        })
        .finally(() => tableLoading.value = false) // 隐藏表格 loading
}

onMounted(() => {
    getTableData()
})

// 查询按钮点击
const handleSearch = () => {
    current.value = 1
    getTableData()
}

// 每页展示数量变更事件
const handleSizeChange = (chooseSize) => {
    size.value = chooseSize
    current.value = 1
    getTableData()
}

// 当前页码变更事件
const handleCurrentChange = (page) => {
    current.value = page
    getTableData()
}

// 删除评论
const deleteCommentSubmit = (row) => {
    showModel('是否确定要该评论，以及其子评论？').then(() => {
        deleteComment(row.id).then((res) => {
            if (!res.success) {
                // 获取服务端返回的错误消息
                let message = res.message
                // 提示错误消息
                showMessage(message, 'error')
                return
            }

            showMessage('删除成功')
            // 重新请求分页接口，渲染数据
            dataLoaded.value = false
            getTableData()
        })
    }).catch((e) => {
    })
}



// 评论详情对话框是否展示
const detailDialogVisible = ref(false)
// 评论数据
const commentDetail = ref({})
// 展示评论详情对话框
const showDetailDialog = (row) => {
    detailDialogVisible.value = true
    if (row.images) {
        row.imagesArray = row.images.split(',').filter(img => img.trim())
    } else {
        row.imagesArray = []
    }
    commentDetail.value = row
}

// 表单引用
const formRef = ref(null)
// 评论审核表单对象
const form = reactive({
    id: null,
    status: '2',
    reason: ''
})

// 规则校验
const rules = {
    status: [
        {
            required: true,
            message: '状态不能为空',
            trigger: 'blur',
        },
    ],
    reason: [
        {
            required: true,
            message: '原因不能为空',
            trigger: 'blur',
        },
    ]
}

// 评论审核对话框引用
const editDialogRef = ref(null)
// 展示评论审核对话框
const showEditDetailDialog = (row) => {
    editDialogRef.value.open()
    // 设置表单对象的评论 ID
    form.id = row.id
}

const onSubmit = () => {
    // 先验证 form 表单字段
    formRef.value.validate((valid) => {
        if (!valid) {
            return false
        }
        
        // 显示提交按钮 loading
        editDialogRef.value.showBtnLoading()
        examineComment(form).then((res) => {
            if (!res.success) {
                // 获取服务端返回的错误消息
                let message = res.message
                // 提示错误消息
                showMessage(message, 'error')
                return
            }

            showMessage('审核完成')
            // 将表单置空
            form.id = null
            form.status = 2
            form.reason = ''
            // 隐藏对话框
            editDialogRef.value.close()
            // 重新请求分页接口，渲染数据
            getTableData()
        }).finally(() => editDialogRef.value.closeBtnLoading()) // 隐藏提交按钮 loading

    })
}
</script>
