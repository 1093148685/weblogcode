
<template>
   <!-- 设置语言为中文 -->
   <el-config-provider :locale="locale">
      <div class="flex flex-col min-h-screen">
         <router-view v-slot="{ Component, route }">
            <transition :name="route.meta.transition || 'fade'" mode="out-in">
               <component :is="Component" :key="route.path" />
            </transition>
         </router-view>
      </div>
   </el-config-provider>
</template>

<script setup>
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
const locale = zhCn

console.log(import.meta.env)
</script>

<style>
/* 自定义顶部加载 Loading 颜色 */
#nprogress .bar {
   background: #409eff!important;
}

/* 暗黑模式 body 背景色 */
.dark body {
   --tw-bg-opacity: 1;
    background-color: rgb(17 24 39 / var(--tw-bg-opacity));
}

/* 路由切换过渡动画 */
.fade-enter-active,
.fade-leave-active {
   transition: opacity 0.15s ease;
}
.fade-enter-from,
.fade-leave-to {
   opacity: 0;
}
</style>
