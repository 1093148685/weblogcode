<template>
  <section class="author-card mb-6 flex items-center gap-4 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
    <img
      class="h-14 w-14 rounded-full object-cover"
      :src="article.authorAvatar || article.avatar || 'https://q1.qlogo.cn/g?b=qq&nk=10000&s=100'"
      alt=""
    >
    <div>
      <div class="mb-1 flex items-center gap-2">
        <strong class="author-card__name text-base font-black text-[#0f172a]">{{ article.author || '测试君' }}</strong>
        <span class="author-card__badge rounded-full bg-blue-50 px-2 py-0.5 text-xs font-bold text-[#2563eb]">作者</span>
      </div>
      <p class="author-card__desc text-[13px] leading-6 text-[#64748b]">专注于分享实用的技术教程与工具。热爱编程，乐于分享，希望能帮到你。</p>
    </div>
  </section>
</template>

<script setup>
defineProps({
  article: { type: Object, default: () => ({}) }
})
</script>

<style scoped>
.author-card {
  border-color: #e8ded0;
  background: rgba(255, 253, 249, 0.92);
  box-shadow: 0 12px 34px rgba(73, 52, 24, 0.06);
}

.author-card__name {
  color: #201b17;
}

.author-card__badge {
  background: #f7f0e6;
  color: #8f6428;
}

.author-card__desc {
  color: #6b6258;
}

:global(html.dark) .author-card {
  border-color: #3a332a;
  background: #1b2027;
  box-shadow: 0 16px 42px rgba(0, 0, 0, 0.2);
}

:global(html.dark) .author-card__name {
  color: #f2eadf;
}

:global(html.dark) .author-card__badge {
  background: rgba(214, 181, 116, 0.14);
  color: #d6b574;
}

:global(html.dark) .author-card__desc {
  color: #afa79c;
}
</style>
