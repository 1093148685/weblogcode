<template>
    <div
        class="tag-nav flex items-center px-3 flex-shrink-0"
        style="height: var(--admin-taglist-height); z-index: var(--z-taglist);"
    >
        <el-tabs
            v-model="activeTab"
            type="card"
            class="nav-tabs flex-1"
            @tab-remove="removeTab"
            @tab-change="tabChange"
        >
            <el-tab-pane
                v-for="item in tabList"
                :key="item.path"
                :label="item.title"
                :name="item.path"
                :closable="item.path != '/admin/index'"
            />
        </el-tabs>

        <el-dropdown @command="handleCloseTab" class="ml-2 flex-shrink-0">
            <div class="nav-action-btn flex items-center justify-center w-[28px] h-[28px] rounded cursor-pointer">
                <el-icon><arrow-down /></el-icon>
            </div>
            <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item command="closeOthers">
                        <el-icon class="mr-1"><Close /></el-icon>关闭其他
                    </el-dropdown-item>
                    <el-dropdown-item command="closeAll">
                        <el-icon class="mr-1"><CircleClose /></el-icon>关闭全部
                    </el-dropdown-item>
                </el-dropdown-menu>
            </template>
        </el-dropdown>
    </div>
</template>

<script setup>
import { useTabList } from '@/composables/useTagList.js'
const { menuStore, activeTab, tabList, tabChange, removeTab, handleCloseTab } = useTabList()
</script>

<style>
.tag-nav {
    background: var(--admin-taglist-bg);
    border-bottom: 1px solid var(--admin-taglist-border);
    box-shadow: var(--admin-shadow-sm);
}

/* Tabs 基础 */
.nav-tabs.el-tabs {
    height: 28px;
}
.nav-tabs .el-tabs__header {
    margin: 0;
    border: none !important;
}
.nav-tabs .el-tabs__nav {
    border: none !important;
}

/* Tab 项 */
.nav-tabs .el-tabs__item {
    height: 26px;
    line-height: 26px;
    padding: 0 10px;
    margin: 0 2px;
    border-radius: 4px;
    border: 1px solid var(--el-border-color);
    background: var(--el-fill-color-light);
    color: var(--el-text-color-secondary);
    font-size: 12px;
    transition: all 0.15s ease;
}

.nav-tabs .el-tabs__item:hover {
    color: var(--el-text-color-primary);
    border-color: var(--el-border-color-light);
}

.nav-tabs .el-tabs__item.is-active {
    background: var(--el-color-primary);
    border-color: var(--el-color-primary);
    color: #fff;
}

/* 关闭按钮 */
.nav-tabs .el-tabs__item .is-icon-close {
    font-size: 10px;
    width: 14px;
    height: 14px;
    margin-left: 3px;
    border-radius: 50%;
    transition: all 0.15s ease;
}

.nav-tabs .el-tabs__item .is-icon-close:hover {
    background: rgba(255, 255, 255, 0.3);
    color: inherit;
}

/* 操作按钮 */
.nav-action-btn {
    transition: all 0.15s ease;
    color: var(--el-text-color-secondary);
}

.nav-action-btn:hover {
    background: var(--el-fill-color);
    color: var(--el-text-color-primary);
}
</style>
