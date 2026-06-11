<template>
    <!-- 外层容器 -->
    <el-container class="admin-layout">

        <!-- 左边侧边栏 -->
        <el-aside :width='menuStore.menuWidth' class="transition-all duration-300 flex-shrink-0 fixed left-0 top-0 h-screen" style="z-index: var(--z-sidebar);">
            <AdminMenu></AdminMenu>
        </el-aside>

        <!-- 右边主内容区域 -->
        <el-container class="right-container" :style="{ paddingLeft: menuStore.menuWidth }">
            <!-- 顶栏容器 -->
            <el-header style="height: var(--admin-header-height); padding: 0;">
                <AdminHeader></AdminHeader>
            </el-header>

            <el-main class="admin-main">
                <!-- 标签导航栏 -->
                <AdminTagList></AdminTagList>

                <!-- 主内容（根据路由动态展示不同页面） -->
                <div class="admin-content">
                    <router-view v-slot="{ Component, route }">
                        <Transition :key="route.fullPath" name="fade" mode="out-in">
                            <KeepAlive :include="cachedViews" :max="10">
                                <component :is="Component" :key="route.fullPath"></component>
                            </KeepAlive>
                        </Transition>
                    </router-view>
                </div>
            </el-main>

            <!-- 底栏容器 -->
            <el-footer style="height: var(--admin-footer-height); padding: 0;">
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

const menuStore = useMenuStore()

const cachedViews = computed(() => {
    return [
        'AdminIndex',
        'AdminArticleList',
        'AdminCategoryList',
        'AdminTagList',
        'AdminBlogSettings',
        'AdminAnnouncement',
        'AdminWikiList',
        'AdminCommentList'
    ]
})

onMounted(() => {
    // 主题由 useDark 管理
})
</script>

<style scoped>
.admin-layout {
    min-height: 100vh;
    background: var(--admin-content-bg);
    color: var(--admin-text);
}

.right-container {
    min-height: 100vh;
    background: var(--admin-content-bg);
    transition: padding-left var(--admin-transition);
}

.el-header {
    background: var(--admin-header-bg);
    border-bottom: 1px solid var(--admin-header-border);
    box-shadow: var(--admin-shadow-sm);
    position: sticky;
    top: 0;
    z-index: var(--z-header);
}

.el-footer {
    padding: 0;
    height: auto;
}

.admin-main {
    display: flex;
    flex-direction: column;
    padding: 0;
    overflow: hidden;
    background: var(--admin-content-bg);
    min-height: calc(100vh - var(--admin-header-height));
}

.admin-content {
    flex: 1;
    overflow-y: auto;
    padding: 20px;
    background: var(--admin-content-bg);
}

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
