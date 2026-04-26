<template>
    <div class="fixed overflow-y-auto h-screen menu-container transition-all duration-300"
         :style="{ width: menuStore.menuWidth }">

        <!-- 顶部 Logo -->
        <div class="logo-area flex items-center justify-center h-[64px] cursor-pointer" @click="router.push('/')">
            <div v-if="menuStore.menuWidth == '250px'" class="flex items-center gap-2">
                <span class="logo-text text-xl font-bold">Weblog</span>
            </div>
            <div v-else class="flex items-center justify-center w-full">
                <span class="logo-text text-lg font-bold">W</span>
            </div>
        </div>

        <!-- 分割线 -->
        <div class="menu-divider mx-3 mb-2"></div>

        <!-- 下方菜单 -->
        <div class="menu-wrapper px-2">
            <el-menu
                :default-active="defaultActive"
                @select="handleSelect"
                :collapse="isCollapse"
                :collapse-transition="false"
            >
                <template v-for="(item, index) in menus" :key="index">
                    <el-sub-menu v-if="item.children" :index="item.path">
                        <template #title>
                            <el-icon class="menu-icon">
                                <component :is="item.icon"></component>
                            </el-icon>
                            <span class="menu-label">{{ item.name }}</span>
                        </template>
                        <el-menu-item
                            v-for="child in item.children"
                            :key="child.path"
                            :index="child.path"
                        >
                            <span class="sub-menu-label">{{ child.name }}</span>
                        </el-menu-item>
                    </el-sub-menu>
                    <el-menu-item v-else :index="item.path">
                        <el-icon class="menu-icon">
                            <component :is="item.icon"></component>
                        </el-icon>
                        <template #title>
                            <span class="menu-label">{{ item.name }}</span>
                        </template>
                    </el-menu-item>
                </template>
            </el-menu>
        </div>

        <!-- 底部版本信息 -->
        <div v-if="menuStore.menuWidth == '250px'" class="menu-footer absolute bottom-0 left-0 right-0 px-4 py-3">
            <div class="text-xs text-slate-500 text-center">Weblog Admin v1.0</div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useMenuStore } from '@/stores/menu'

const menuStore = useMenuStore()

const route = useRoute()
const router = useRouter()

// 是否折叠
const isCollapse = computed(() =>  !(menuStore.menuWidth == '250px'))

// 根据路由地址判断哪个菜单���选中
const defaultActive = ref(route.path)

// 菜单选择事件
const handleSelect = (path) => {
    router.push(path)
}

const menus = [
    {
        'name': '仪表盘',
        'icon': 'Monitor',
        'path': '/admin/index'
    },
    {
        'name': '文章管理',
        'icon': 'Document',
        'path': '/admin/article/list',
    },
    {
        'name': '分类管理',
        'icon': 'FolderOpened',
        'path': '/admin/category/list',
    },
    {
        'name': '标签管理',
        'icon': 'PriceTag',
        'path': '/admin/tag/list',
    },
    {
        'name': '知识库管理',
        'icon': 'Collection',
        'path': '/admin/wiki/list',
    },
    {
        'name': '评论管理',
        'icon': 'ChatDotSquare',
        'path': '/admin/comment/list',
        'children': [
            {
                'name': '评论列表',
                'path': '/admin/comment/list',
            },
            {
                'name': '私密评论',
                'path': '/admin/comment/secret',
            },
            {
                'name': '贴纸管理',
                'path': '/admin/sticker',
            }
        ]
    },
    {
        'name': '博客设置',
        'icon': 'Setting',
        'path': '/admin/blog/settings',
    },
    {
        'name': '公告管理',
        'icon': 'Bell',
        'path': '/admin/announcement',
    },
    {
        'name': 'AI 管理',
        'icon': 'Cpu',
        'path': '/admin/ai',
        'children': [
            {
                'name': 'AI 模型',
                'path': '/admin/ai-model',
            },
            {
                'name': 'AI Provider',
                'path': '/admin/ai-provider',
            },
            {
                'name': 'AI 插件',
                'path': '/admin/ai-plugin',
            },
            {
                'name': 'RAG 知识库',
                'path': '/admin/knowledge-base',
            },
            {
                'name': 'Prompt 模板',
                'path': '/admin/ai/prompt-list',
            },
            {
                'name': '写作助手',
                'path': '/admin/ai-assistant',
            },
            {
                'name': '智能 Agent',
                'path': '/admin/agent',
            },
        ]
    },
]
</script>

<style>
.menu-container {
    background:
        radial-gradient(circle at 20% 0%, rgba(99, 102, 241, 0.22), transparent 35%),
        linear-gradient(180deg, #0a0f1c 0%, #0d1320 52%, #070b14 100%);
    border-right: 1px solid rgba(148, 163, 184, 0.14);
    box-shadow: 14px 0 45px rgba(0, 0, 0, 0.32);
    display: flex;
    flex-direction: column;
    z-index: 100;
    position: relative;
    overflow: hidden;
}

/* ===== Logo 区域 ===== */
.logo-area {
    background: rgba(255, 255, 255, 0.025);
    border-bottom: 1px solid rgba(148, 163, 184, 0.12);
    transition: background 0.2s ease;
}

.logo-area:hover {
    background: rgba(99, 102, 241, 0.12);
}

.logo-text {
    background: linear-gradient(135deg, #f8fafc 0%, #67e8f9 48%, #a78bfa 100%);
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
    letter-spacing: -0.02em;
}

/* ===== 分割线 ===== */
.menu-divider {
    height: 1px;
    background: linear-gradient(90deg, transparent, rgba(148, 163, 184, 0.18), transparent);
}

/* ===== 菜单整体 ===== */
.el-menu {
    background-color: transparent !important;
    border-right: 0 !important;
}

/* ===== 一级菜单项 ===== */
.el-menu-item {
    color: #94a3b8 !important;
    border-radius: 12px !important;
    margin: 3px 6px !important;
    height: 44px !important;
    line-height: 44px !important;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1) !important;
    position: relative;
    overflow: hidden;
}

.el-menu-item::before {
    content: '';
    position: absolute;
    left: 0;
    top: 50%;
    transform: translateY(-50%) scaleY(0);
    width: 3px;
    height: 0;
    background: linear-gradient(180deg, #22d3ee, #8b5cf6);
    border-radius: 0 3px 3px 0;
    transition: all 0.2s ease;
    opacity: 0;
}

.el-menu-item:hover {
    background: rgba(99, 102, 241, 0.14) !important;
    color: #f8fafc !important;
}

.el-menu-item:hover::before {
    opacity: 0.5;
    height: 40%;
    transform: translateY(-50%) scaleY(1);
}

.el-menu-item.is-active {
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.22), rgba(34, 211, 238, 0.12)) !important;
    color: #ffffff !important;
    box-shadow: inset 0 0 0 1px rgba(148, 163, 184, 0.12);
}

.el-menu-item.is-active::before {
    opacity: 1;
    height: 60%;
    transform: translateY(-50%) scaleY(1);
}

.el-menu-item.is-active:hover {
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.28), rgba(34, 211, 238, 0.16)) !important;
}

/* ===== 子菜单标题 ===== */
.el-sub-menu__title {
    color: #94a3b8 !important;
    border-radius: 12px !important;
    margin: 3px 6px !important;
    height: 44px !important;
    line-height: 44px !important;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

.el-sub-menu__title:hover {
    background: rgba(99, 102, 241, 0.14) !important;
    color: #f8fafc !important;
}

.el-sub-menu.is-opened > .el-sub-menu__title {
    color: #f8fafc !important;
}

/* ===== 子菜单容器 ===== */
.el-sub-menu .el-menu {
    background-color: transparent !important;
    padding: 4px 0;
}


/* ===== 子菜单项 ===== */
.el-sub-menu .el-menu-item {
    color: #7f8da3 !important;
    font-size: 13px !important;
    min-width: unset !important;
    height: 38px !important;
    line-height: 38px !important;
    padding-left: 48px !important;
    border-radius: 8px !important;
    margin: 1px 0 !important;
}

.el-sub-menu .el-menu-item:hover {
    background: rgba(99, 102, 241, 0.12) !important;
    color: #f8fafc !important;
}

.el-sub-menu .el-menu-item.is-active {
    background: rgba(34, 211, 238, 0.12) !important;
    color: #ffffff !important;
}

/* ===== 图标 ===== */
.menu-icon {
    font-size: 17px !important;
    margin-right: 2px;
    opacity: 0.85;
}

.el-menu-item.is-active .menu-icon,
.el-sub-menu.is-opened .menu-icon {
    opacity: 1;
    color: #67e8f9;
}

/* ===== 菜单标签文字 ===== */
.menu-label {
    font-size: 14px;
    font-weight: 500;
    letter-spacing: 0.01em;
}

.sub-menu-label {
    font-size: 13px;
    font-weight: 400;
}

/* ===== 折叠箭头 ===== */
.el-sub-menu__icon-arrow {
    color: #475569 !important;
}

/* ===== 底部信息 ===== */
.menu-footer {
    background: rgba(2, 6, 23, 0.42);
    border-top: 1px solid rgba(148, 163, 184, 0.12);
}

/* ===== 菜单项悬停和激活时的图标动画 ===== */
.el-menu-item .menu-icon,
.el-sub-menu__title .menu-icon {
    transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1), color 0.2s ease, opacity 0.2s ease;
    opacity: 0.8;
}

.el-menu-item:hover .menu-icon,
.el-sub-menu__title:hover .menu-icon {
    transform: scale(1.12);
    opacity: 1;
}

.el-menu-item.is-active .menu-icon,
.el-sub-menu.is-opened .menu-icon {
    opacity: 1;
    color: #67e8f9;
}

/* ===== 激活菜单项的左侧指示器动画 ===== */
.el-menu-item.is-active::before {
    transition: height 0.2s ease, background 0.2s ease;
}

/* ===== 优化子菜单项的内边距，让层级更清晰 ===== */
.el-sub-menu .el-sub-menu .el-menu-item {
    padding-left: 60px !important;
}
</style>
