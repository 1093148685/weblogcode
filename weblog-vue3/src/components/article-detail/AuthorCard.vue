<template>
  <section class="mb-6 flex items-center gap-4 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
    <img
      class="h-14 w-14 rounded-full object-cover"
      :src="article.authorAvatar || article.avatar || 'https://q1.qlogo.cn/g?b=qq&nk=10000&s=100'"
      alt=""
    >
    <div>
      <div class="mb-1 flex items-center gap-2">
        <strong class="text-base font-black text-[#0f172a]">{{ article.author || '测试君' }}</strong>
        <span class="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-bold text-[#2563eb]">作者</span>
      </div>
      <p class="text-[13px] leading-6 text-[#64748b]">专注于分享实用的技术教程与工具。热爱编程，乐于分享，希望能帮到你。</p>
    </div>
  </section>
</template>

<script setup>
defineProps({
  article: { type: Object, default: () => ({}) }
})
</script>
