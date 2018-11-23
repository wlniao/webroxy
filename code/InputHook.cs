using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TcpRouter
{
    /// <summary>
    /// 传入的webhook信息
    /// </summary>
    public class InputHook
    {
        public string before { get; set; }
        public string after { get; set; }
        public Repository repository { get; set; }
        public List<Commits> commits { get; set; }


        #region 信息获取
        /// <summary>
        /// 仓库名称
        /// </summary>
        public String RepositoryName()
        {
            if (repository != null)
            {
                if (!string.IsNullOrEmpty(repository.name))
                {
                    return repository.name;
                }
            }
            return "";
        }
        /// <summary>
        /// 仓库地址
        /// </summary>
        public String RepositoryUrl()
        {
            if (repository != null && !string.IsNullOrEmpty(repository.url))
            {
                return repository.url;
            }
            return "";
        }

        /// <summary>
        /// 提交人
        /// </summary>
        public String AuthorName()
        {
            if (commits != null)
            {
                foreach (var commit in commits)
                {
                    if (commit.author != null && !string.IsNullOrEmpty(commit.author.name))
                    {
                        return commit.author.name;
                    }
                    if (commit.committer != null && !string.IsNullOrEmpty(commit.committer.name))
                    {
                        return commit.committer.name;
                    }
                }
            }
            return "";
        }
        /// <summary>
        /// 提交人邮箱
        /// </summary>
        public String AuthorEmail()
        {
            if (commits != null)
            {
                foreach (var commit in commits)
                {
                    if (commit.author != null && !string.IsNullOrEmpty(commit.author.email))
                    {
                        return commit.author.email;
                    }
                    if (commit.committer != null && !string.IsNullOrEmpty(commit.committer.email))
                    {
                        return commit.committer.email;
                    }
                }
            }
            return "";
        }
        /// <summary>
        /// Push时填写的Commit备注
        /// </summary>
        public string CommitMessage()
        {
            if (commits != null)
            {
                foreach(var commit in commits)
                {
                    if (!string.IsNullOrEmpty(commit.message))
                    {
                        return commit.message;
                    }
                    if (!string.IsNullOrEmpty(commit.short_message))
                    {
                        return commit.short_message;
                    }
                }
            }
            return "";
        }
        #endregion

        #region 序列化实体
        public class Repository
        {
            public string url { get; set; }
            public string name { get; set; }
        }
        public class Author
        {
            public string name { get; set; }
            public string email { get; set; }
        }
        public class Commits
        {
            public string id { get; set; }
            public string message { get; set; }
            public string short_message { get; set; }
            public Author author { get; set; }
            public Author committer { get; set; }
        }
        #endregion
    }
}
