<template>
    <!-- 外层容器 -->
    <el-container class="admin-layout">

        <!-- 左边侧边栏 -->
        <el-aside :width='menuStore.menuWidth' class="transition-all duration-300 flex-shrink-0 fixed left-0 top-0 h-screen z-50">
            <AdminMenu></AdminMenu>
        </el-aside>

        <!-- 右边主内容区域 -->
        <el-container class="right-container" :style="{ paddingLeft: menuStore.menuWidth }">
            <!-- 顶栏容器 -->
            <el-header>
                <AdminHeader></AdminHeader>
            </el-header>

            <el-main class="admin-main">
                <!-- 标签导航栏 -->
                <AdminTagList></AdminTagList>

                <!-- 主内容（根据路由动态展示不同页面） -->
                <router-view v-slot="{ Component, route }">
                    <Transition name="fade" mode="out-in">
                        <KeepAlive :include="cachedViews" :max="10" :key="route.path">
                            <component :is="Component"></component>
                        </KeepAlive>
                    </Transition>
                </router-view>
            </el-main>

            <!-- 底栏容器 -->
            <el-footer>
                <AdminFooter></AdminFooter>
            </el-footer>
        </el-container>
    </el-container>
</template>

<script setup>
import AdminFooter from './components/AdminFooter.vue';
import AdminHeader from './components/AdminHeader.vue';
import AdminMenu from './components/AdminMenu.vue';
import AdminTagList from './components/AdminTagList.vue';
import { onMounted, computed } from 'vue';
import { useMenuStore } from '@/stores/menu'
import { useThemeStore } from '@/stores/theme'

const menuStore = useMenuStore()
const themeStore = useThemeStore()

const cachedViews = computed(() => {
    return [
        'AdminIndex',
        'AdminArticleList',
        'AdminCategoryList',
        'AdminTagList',
        'AdminBlogSettings',
        'AdminAnnouncement',
        'AdminAiModel',
        'AdminAiPlugin',
        'AdminAiProvider',
        'AdminWikiList',
        'AdminCommentList'
    ]
})

onMounted(() => {
    // 恢复持久化的主题设置
    themeStore.init()
})
</script>

<style scoped>
.admin-layout {
    min-height: 100vh;
    background: var(--admin-bg-page);
}

.right-container {
    min-height: 100vh;
    background: var(--admin-bg-page);
}

.el-header {
    padding: 0 !important;
    height: 64px;
}

.el-footer {
    padding: 0 !important;
    height: auto;
}

.admin-main {
    padding: 0;
    background: var(--admin-bg-page);
    min-height: calc(100vh - 64px - 44px - 50px);
    overflow-x: visible !important;
    overflow-y: visible !important;
}

/* 内容区域过渡动画 */
.fade-enter-active {
    transition: all 0.25s ease-out;
}

.fade-leave-active {
    transition: all 0.15s ease-in;
}

.fade-enter-from {
    opacity: 0;
    transform: translateY(8px);
}

.fade-leave-to {
    opacity: 0;
}
</style>
